"""
training_utils.py — Shared training infrastructure for all RL agent notebooks.

Provides:
  - DRCurriculumCallback: gradually ramps DR noise during Phase 3
  - MaskableEvalCB: evaluation callback compatible with MaskablePPO action masking
  - cosine_lr_fn: cosine-annealed learning rate schedule
  - 3-phase hyperparameter constants (easy maps → full maps → DR)
"""

import os
import math
import numpy as np
from stable_baselines3.common.callbacks import BaseCallback


PHASE1_STEPS = 5_000_000
PHASE2_STEPS = 5_000_000
PHASE3_STEPS = 4_000_000

PHASE1_WIN_FLOOR = 8

P1_LR_START = 3e-4
P1_LR_END = 3e-5
P1_N_EPOCHS = 6
P1_ENT_COEF = 0.05
P1_CLIP_RANGE = 0.2

P2_LR_START = 1e-4
P2_LR_END = 1e-5
P2_N_EPOCHS = 5
P2_ENT_COEF = 0.05
P2_CLIP_RANGE = 0.2

P3_LR_START = 3e-5
P3_LR_END = 5e-6
P3_N_EPOCHS = 4
P3_ENT_COEF = 0.08
P3_CLIP_RANGE = 0.15

P3_DR_RAMP_STEPS = 500_000
P3_DR_TARGET = 0.08

P1_EVAL_FREQ = 100_000
P1_EVAL_EPISODES = 20
P2_EVAL_FREQ = 100_000
P2_EVAL_EPISODES = 20
P3_EVAL_FREQ = 50_000
P3_EVAL_EPISODES = 20


def cosine_lr_fn(lr_start: float, lr_end: float):
    """Returns a callable schedule: progress (1→0) → learning rate."""
    def _schedule(progress_remaining: float) -> float:
        t = 1.0 - progress_remaining
        cosine_decay = 0.5 * (1.0 + math.cos(math.pi * t))
        return lr_end + (lr_start - lr_end) * cosine_decay
    return _schedule


class DRCurriculumCallback(BaseCallback):
    """Gradually ramps DR noise from 0% → target% over `ramp_steps` timesteps.

    Must be attached to a model whose training env wraps a RoguelikeEnv
    (or subclass) that exposes `set_dr_noise()`.  Works with SubprocVecEnv
    by calling `env_method`.
    """

    def __init__(
        self,
        target_noise: float = P3_DR_TARGET,
        ramp_steps: int = P3_DR_RAMP_STEPS,
        update_every: int = 50_000,
        verbose: int = 0,
    ):
        super().__init__(verbose)
        self.target_noise = target_noise
        self.ramp_steps = ramp_steps
        self.update_every = update_every
        self._last_update = 0
        self._start_ts = 0

    def _on_training_start(self):
        self._start_ts = self.num_timesteps

    def _on_step(self) -> bool:
        elapsed = self.num_timesteps - self._start_ts
        if elapsed - self._last_update >= self.update_every:
            self._last_update = elapsed
            progress = min(elapsed / max(self.ramp_steps, 1), 1.0)
            noise = self.target_noise * progress
            try:
                self.training_env.env_method("set_dr_noise", noise)
            except AttributeError:
                env = self.training_env
                while hasattr(env, 'env'):
                    env = env.env
                if hasattr(env, 'set_dr_noise'):
                    env.set_dr_noise(noise)
            if self.verbose >= 1:
                print(f"   DR noise ramped to {noise:.4f} ({progress*100:.0f}%)")
        return True


class MaskableEvalCB(BaseCallback):
    """EvalCallback that passes action_masks to MaskablePPO.predict().

    Unlike the stock EvalCallback, this correctly handles action masking
    during deterministic evaluation — critical for masked PPO agents.
    """

    def __init__(
        self,
        eval_env,
        best_model_save_path: str,
        log_path: str | None = None,
        eval_freq: int = 100_000,
        n_eval_episodes: int = 20,
        deterministic: bool = True,
        verbose: int = 1,
    ):
        super().__init__(verbose)
        self.eval_env = eval_env
        self.best_model_save_path = best_model_save_path
        self.log_path = log_path
        self.eval_freq = eval_freq
        self.n_eval_episodes = n_eval_episodes
        self.deterministic = deterministic
        self.best_mean_reward = -np.inf
        self._last_eval_ts = 0

    def _on_step(self) -> bool:
        if self.num_timesteps - self._last_eval_ts >= self.eval_freq:
            self._last_eval_ts = self.num_timesteps
            self._run_eval()
        return True

    def _run_eval(self):
        rewards, ep_lens = [], []
        for _ in range(self.n_eval_episodes):
            obs, _ = self.eval_env.reset()
            done, ep_r, ep_l = False, 0.0, 0
            while not done:
                masks = self.eval_env.action_masks()
                act, _ = self.model.predict(
                    obs, deterministic=self.deterministic, action_masks=masks
                )
                obs, r, term, trunc, _ = self.eval_env.step(int(act))
                ep_r += r
                ep_l += 1
                done = term or trunc
            rewards.append(ep_r)
            ep_lens.append(ep_l)

        mean_r = float(np.mean(rewards))
        std_r = float(np.std(rewards))
        mean_len = float(np.mean(ep_lens))

        if self.verbose >= 1:
            print(f"   Eval @{self.num_timesteps:,}: reward={mean_r:.1f} +/-{std_r:.1f}, ep_len={mean_len:.0f}")

        if mean_r > self.best_mean_reward:
            self.best_mean_reward = mean_r
            os.makedirs(self.best_model_save_path, exist_ok=True)
            self.model.save(os.path.join(self.best_model_save_path, "best_model"))
            if self.verbose >= 1:
                print(f"   New best! {mean_r:.1f} -- model saved.")
