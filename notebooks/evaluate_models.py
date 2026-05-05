"""
Quick evaluation script to test trained RL agent models locally.
Usage: python evaluate_models.py [agent_name] [num_episodes]
Example: python evaluate_models.py aggressive 50
"""
import sys
import os

# Add project root to path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..'))

from sb3_contrib import MaskablePPO
from python_roguelike.env.roguelike_env import RoguelikeEnv

MODELS_DIR = os.path.join(os.path.dirname(__file__), 'models')

AGENTS = {
    'aggressive': 'aggressive_final.zip',
    'balanced':   'balanced_final.zip',
    'defensive':  'defensive_final.zip',
    'adaptive':   'adaptive_final.zip',
}


def evaluate_agent(agent_name: str, num_episodes: int = 50):
    model_path = os.path.join(MODELS_DIR, AGENTS[agent_name])
    if not os.path.exists(model_path):
        print(f"Model not found: {model_path}")
        return

    print(f"Loading {agent_name} model from {model_path}...")
    model = MaskablePPO.load(model_path)

    env = RoguelikeEnv()
    wins, deaths = 0, 0
    floors_on_death, hp_pcts = [], []
    floor_deaths = [0] * 17  # floors 0-16

    for ep in range(num_episodes):
        obs, _ = env.reset()
        done = False
        while not done:
            action_masks = env.action_masks()
            action, _ = model.predict(obs, deterministic=True, action_masks=action_masks)
            obs, reward, terminated, truncated, info = env.step(int(action))
            done = terminated or truncated

        # Check win/loss via HP (info has no 'won' key)
        hero_hp = info.get('hp', 0)
        hero_max = info.get('max_hp', 1)
        floor = info.get('floor', 0)

        if hero_hp > 0:
            wins += 1
            hp_pcts.append(hero_hp / hero_max * 100)
        else:
            deaths += 1
            floors_on_death.append(floor)
            if 0 <= floor < len(floor_deaths):
                floor_deaths[floor] += 1

    env.close()

    print(f"\n{agent_name.capitalize()} Agent Results ({num_episodes} episodes)")
    print("=" * 50)
    print(f"  Win Rate:     {wins/num_episodes*100:.1f}%  ({wins}/{num_episodes})")
    print(f"  Death Rate:   {deaths/num_episodes*100:.1f}%")
    if floors_on_death:
        print(f"  Avg Floor:    {sum(floors_on_death)/len(floors_on_death):.1f} / 15")
    print(f"  Deaths by floor: {floor_deaths}")
    if hp_pcts:
        print(f"  Avg HP% on Win: {sum(hp_pcts)/len(hp_pcts):.1f}%")


if __name__ == '__main__':
    agent = sys.argv[1] if len(sys.argv) > 1 else 'all'
    n_eps = int(sys.argv[2]) if len(sys.argv) > 2 else 50

    if agent == 'all':
        for name in AGENTS:
            evaluate_agent(name, n_eps)
            print()
    elif agent in AGENTS:
        evaluate_agent(agent, n_eps)
    else:
        print(f"Unknown agent: {agent}. Choose from: {list(AGENTS.keys())} or 'all'")
