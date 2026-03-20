# Báo cáo hàng tuần

**BÁO CÁO TIẾN ĐỘ ĐỒ ÁN TỐT NGHIỆP**

**Học kỳ:** Học kỳ II năm học 2025-2026  
**Sinh viên thực hiện:** Nguyễn Nhật Phong  
**MSSV:** 22028272  
**Lớp:** QH-2022-I/CQ-I-CS2  
**Giảng viên hướng dẫn:** PGS. TS. Nguyễn Trí Thành

---

# I. THÔNG TIN CHUNG

Tên đề tài: 

* Tiếng Anh: *Optimizing Game Balance in Roguelike Deckbuilders via Multi-Objective Evolutionary Algorithms and Reinforcement Learning*  
* Tiếng Việt: *Tối ưu hóa cân bằng game Roguelike Deckbuilder sử dụng giải thuật di truyền đa mục tiêu và học tăng cường*

Link Github: [xcviixix-rei/Roguelike](https://github.com/xcviixix-rei/Roguelike)

---

# II. KẾ HOẠCH TỔNG THỂ (DỰ KIẾN)

| Giai đoạn | Thời gian | Nội dung chính | Trạng thái |
| :---- | :---- | :---- | :---- |
| 1 | Tuần 1 \- 3 | Nghiên cứu lý thuyết RL, review Papers, thiết lập môi trường | Đang thực hiện |
| 2 | Tuần 4 \- 8 | Triển khai các agent RL (DQN/PPO) với behaviors khác nhau | Đang thực hiện |
| 3 | Tuần 9 \- 11 | Tích hợp CMA-ES và NSGA-II để tối ưu tham số | Chưa bắt đầu |
| 4 | Tuần 12 \- 15 | Thực nghiệm, đánh giá kết quả, viết báo cáo khóa luận | Chưa bắt đầu |

### 

# Tuần 7 \+ 8 \+ 9

1. ## Tuần 7 \+ 8 \+ 9: từ ngày 10/2 đến 1/3

   1. ### Mục tiêu các tuần này

- Tìm kiếm thêm về các agent chơi game deck builder tương tự theo hướng aggressive (trước kia em có tìm hiểu nhưng chỉ tìm thấy các bài về agent theo hướng phòng thủ hoặc theo hướng balance)  
- Thiết kế thêm các vật phẩm hồi máu cho game (hỗ trợ agent có thể vừa gây nhiều dmg mà không lo thiếu máu)

  2. ### Kết quả

- Nghiên cứu lý thuyết và định hình lại aggressive agent: Sau khi tìm hiểu kỹ hơn các lý thuyết về Rl trong game như reward hacking, em đã rút ra một số insight để sửa lỗi agent tự sát:  
+ Tái định nghĩa lối chơi aggressive phạt thời gian thay vì thưởng sát thương. Việc em định nghĩa hàm reward thưởng cho lượng sát thương gây ra là quá dày và khó điều chỉnh, thay vào đó thì sẽ phạt agent mỗi một lượt đánh bài đi qua để nó kết thúc nhanh nhất có thể  
+ Công thức mới: Thắng và vượt qua mỗi tầng \+30, thắng game hoàn toàn \+50 nữa, thua game \-100, \-1 điểm mỗi một lượt chơi bài. Lý do em chia như thế này vì đối với bản thân em tự chơi sẽ mất trung bình khoảng 250 đến 300 lượt đánh để thắng game, nếu để như trước thì agent rất khó học để chơi được đến cuối và nhận thưởng \+50 thắng \-\> em chia nhỏ phần thưởng cho từng tầng, cùng với scale lên để cho phù hợp với mức trừ 1 điểm mỗi lượt  
- Bổ sung mechanics (vật phẩm hồi máu)  
+ Vấn đề: Chơi aggressive đồng nghĩa với việc bỏ qua phòng thủ và nhận nhiều sát thương. Với lượng HP cố định và ít phòng hồi máu, agent chắc chắn sẽ chết trước khi phá đảo 15 tầng dù chiến thuật có tối ưu đến đâu  
+ Cập nhật: Bổ sung thêm một phần mới là vật phẩm hồi máu. Vật phẩm này nhặt được sau khi vượt qua một phòng, có thể hồi số máu bằng với độ khó của phòng sinh ra nó (phòng độ khó 1 sao sẽ cho ra bình hồi 1 HP, phòng 2 sao cho bình hồi 2 HP). Nếu trong một lượt mà agent đã đánh bài thì không thể dùng bình máu và ngược lại. Agent sẽ phải cân nhắc thời điểm dùng bình máu khi kẻ địch không tấn công  
+  Vấn đề: Tăng thêm 1 action trong action space, em cần phải update lại gym wrapper trước đó

  3. ### Kế hoạch tuần tới

- Cập nhật action space bên python để nhận diện được vật phẩm hồi máu từ C\#  
- Retrain phase 2 với cơ chế reward mới (tập trung vào turn penalty) và hệ thống máu mới  
- Tiếp tục đọc các tài liệu và paper liên quan

# Tuần 6

1. ## Tuần 6: từ ngày 2/1 đến 9/2

   1. ### Mục tiêu tuần này

- Training phase 1: Chạy aggressive agent trên kaggle GPU  
- Fine-tuning: Theo dõi các chỉ số bằng tensorboard (value loss, entropy) để tinh chỉnh  
- Evaluation: Đánh giá hiệu năng agent dựa trên 2 tiêu chí win rate và average turn per win

  2. ### Kết quả

- Kết quả Training (Chưa đạt kỳ vọng):  
+ Dự định chạy thử nghiệm 1 triệu timesteps với reward function thiết kế ở tuần 5  
+ Hiện tượng:  
* Reward mean: Tăng nhẹ ở giai đoạn đầu nhưng nhanh chóng bão hòa ở mức thấp  
* Winrate: Dao động quanh mức 8-12%  
* Episode length: Rất ngắn (trung bình 30 turns/game, agent chơi được đến khoảng tầng 4/15 tầng là chết)  
- Phân tích nguyên nhân:  
+ Mất cân bằng điểm thưởng: Cơ chế hiện tại là \+0.1 điểm/1% máu địch mất đi \-\> Giết 1 quái (100%) được \+10 điểm. Trung bình 1 room nhóm tầng dưới có khoảng 1,25 quái \-\> agent kiếm được 12.5 điểm/room dẫn đến agent học được policy chơi 4 room (kiếm 50 điểm) rồi thua (-10 điểm phạt) , tổng kết được 40 điểm.\<br\>  
+ Kết luận: Agent nhận thấy việc kiếm damage ở các tầng thấp rồi thua mang lại rewward cao hơn so với việc cố gắng sống sót qua các room khó để thắng game (+20 điểm). Hàm phạt thua cuộc chỉ trừ 10 điểm là quá nhẹ so với lượng điểm damage kiếm được  
- Thiết kế lại hàm phần thưởng:  
+ Hệ thống phòng của game có độ khó từ 1 đến 5, thay vì cộng 0.1 điểm/1% máu địch mất đi như cũ thì em chia 5 ra thành 0.02/1% máu địch, sau đó nhân với hệ số độ khó  
+ Viết thêm một khoản thưởng tiến trình: mỗi tầng leo lên sẽ được cộng thêm 5 điểm  
+ Do reward thắng thua cũ hơi nhỏ nên em scale lại, cho phần thưởng thắng là \+50, thua là \-50

  3. ### Kế hoạch tuần tới

- Tìm kiếm thêm về các agent chơi game deck builder tương tự theo hướng aggressive (trước kia em có tìm hiểu nhưng chỉ tìm thấy các bài về agent theo hướng phòng thủ hoặc theo hướng balance)  
- Thiết kế thêm các vật phẩm hồi máu cho game (hỗ trợ agent có thể vừa gây nhiều dmg mà không lo thiếu máu)

# Tuần 5

2. ## Tuần 5: từ ngày 26/1 đến 1/2

   1. ### Mục tiêu tuần này

- Tích hợp thuật toán MaskablePPO (thư viện sb3-contrib) vào môi trường Gym  
- Thiết kế và cài đặt hàm thưởng chuyên biệt cho aggressive agent  
- Xây dựng module đánh giá và thực hiện Unit Test hệ thống trước khi training diện rộng

  2. ### Kết quả

- Triển khai MaskablePPO (action masking):  
+ Thay thế PPO tiêu chuẩn bằng MaskablePPO  
+ Cơ chế hoạt động: Tại mỗi bước step(), python gọi sang thư viện C\# để lấy danh sách các hành động hợp lệ, sau đó chuyển đổi thành binary mask gửi vào mạng nơ ron  
+ Kết quả: Agent hiện tại tuyệt đối không đưa ra các quyết định sai luật (như đánh bài khi thiếu mana, chọn sai mục tiêu)  
- Thiết kế reward function cho aggressive agent:  
+ Đã cụ thể hóa hành vi aggressive (chú trọng tấn công) thành công thức toán học:  
* Normalized damage reward: \+ 0.1 điểm cho mỗi 1% máu tối đa của địch bị mất (thay vì tính theo điểm damage tuyệt đối, giúp ổn định scale điểm giữa đầu game và cuối game)  
* Turn penalty: \- 0.5 điểm cho mỗi lượt đi trôi qua  
* Victory Bonus: \+ 20 điểm khi thắng  
* Defeat Penalty: \- 10 điểm khi thua   
* Chiến lược cho agent: chấp nhận rủi ro mất máu để dồn damage kết thúc nhanh hơn là đánh an toàn mà kéo dài  
- Unit Testing và validation (sanity check):  
+ Test masking: Đã chạy thử 100 bước ngẫu nhiên, xác nhận không có bất kỳ hành động sai luật nào được thực thi (Mask hoạt động chính xác 100%)  
+ Test reward: Kiểm tra log reward trả về, agent nhận điểm dương khi gây damage và điểm âm khi đánh phòng thủ câu giờ  
- Evaluation module: Xây dựng xong module đánh giá độc lập theo dõi: ưin rate và average turn per win

  3. ### Kế hoạch tuần tới

- Training phase 1: Chạy aggressive agent trên kaggle GPU  
- Fine-tuning: Theo dõi các chỉ số bằng tensorboard (value loss, entropy) để tinh chỉnh  
- Evaluation: Đánh giá hiệu năng agent dựa trên 2 tiêu chí win rate và average turn per win

# Tuần 4

3. ## Tuần 4: từ ngày 19/1 đến 25/1

   1. ### Mục tiêu tuần này

- Map toàn bộ logic từ C\# DLL vào hàm step() của python (thay thế mock logic hiện tại)  
- Viết script chuyển đổi dữ liệu cấu hình game từ json gốc sang cấu trúc runtime của python  
- Tìm hiểu các nghiên cứu liên quan đến tối ưu PPO agent cho game tương tự

  2. ### Kết quả

- Hoàn thành full system integration giữa C\# với python:  
+ Đã thay thế mock logic tuần trước sang gọi trực tiếp API từ Roguelike.Core.dll  
+ Đã ánh xạ thành công các hàm như PlayCard(), EndPlayerTurn(),...  từ C\# sang env.step() của python  
+ Validation: Dữ liệu trả về khớp tuyệt đối với logic game gốc khi chạy trên dotnet console   
- Dynamic data pipeline:  
+ Hoàn thành script chuyển đổi file json cấu hình game thành python runtime objects  
+ Tích hợp thành công module tạo nhiễu vào pipeline load dữ liệu. Mỗi khi env.reset(), hệ thống tự động tạo ra một biến thể game mới với khoảng random dữ liệu giống với khoảng cho phép chỉnh sửa của thuật toán GA em đã làm, ép agent học chiến thuật tổng quát  
- Nghiên cứu & cải tiến thuật toán (dựa trên paper [A Closer Look at Invalid Action Masking in Policy Gradient Algorithms](https://arxiv.org/pdf/2006.14171)):  
+ Ý tưởng cốt lõi: Trong các game có không gian hành động lớn nhưng bị ràng buộc bởi luật, việc để agent tự học không được đi nước sai là lãng phí. Masking (che đi hành động sai) được chứng minh toán học là tương đương với một valid policy gradient update  
+ Vấn đề: Game Deckbuilder có rất nhiều ràng buộc (không đủ mana, không có mục tiêu, bài chưa rút xong). Agent PPO gốc mất rất nhiều thời gian để học được các luật rất cơ bản như kiểu “không đủ mana để đánh lá bài này"  
+ Giải pháp: Áp dụng Invalid Action Masking. Hệ thống C\# sẽ trả về một mask bit (0/1) cho biết hành động nào khả thi. Mask này sẽ triệt tiêu Logits của hành động sai trước khi đưa vào hàm Softmax, giúp Agent hội tụ nhanh hơn nhiều lần

  3. ### Kế hoạch tuần tới

- Tích hợp MaskablePPO (từ thư viện sb3-contrib) vào môi trường Gym  
- Tập trung hoàn thiện thiết kế aggressive agent: cụ thể hóa công thức reward cho hành vi aggressive (ví dụ như tăng trọng số cho avg damage/turn, phạt nặng nếu trận đấu kéo dài)  
- Unit test agent trên môi trường thực (chưa train) để đảm bảo mask hoạt động đúng và reward trả về đúng ý đồ thiết kế  
- Xây dựng module đánh giá agent dựa trên winrate trước tiên

# Tuần 3

4. ## Tuần 3: từ ngày 12/1 đến 18/1

   1. ### Mục tiêu tuần này

- Xây dựng pipeline tự động để chạy C\# game logic (.NET 9\) trên kaggle kernel (Linux)  
- Hoàn thiện lớp DotNetGameEnv chuẩn OpenAI Gymnasium  
- Kiểm thử khả năng tương tác giữa python và C\# DLLs  
- Phân tích khả năng ứng dụng của Domain Randomization vào bài toán cân bằng game

  2. ### Kết quả

- Thành công tích hợp Cross-platform (.NET 9 on Linux):  
+ Vấn đề: Kaggle chạy Linux container, không có sẵn .NET runtime và khó config pythonnet  
+ Giải pháp: Viết script tự động tải và cài đặt .NET 9.0 SDK, cấu hình biến môi trường DOTNET\_ROOT và pythonnet CoreCLR runtime ngay trong Notebook  
+ Kết quả: Python đã load thành công Roguelike.Core.dll build từ C\#, khởi tạo được object (CardData, Random) và gọi hàm với tốc độ native  
- Xây dựng môi trường RL (Gym Interface):  
+ Đã implement class DotNetGameEnv với đầy đủ chuẩn:  
* Action space: Discrete(51) (50 slots đánh bài \+ 1 end turn)  
* Observation space: Box(77) (Vector hóa thông tin hero, hand, enemies, game state)  
* Reward functions: Đã thiết kế sẵn 4 hàm reward cho 4 behavior: aggressive (tối ưu dmg), defensive (tối ưu giáp), balanced và adaptive  
- Unit testing:  
+ Đã chạy thử nghiệm khởi tạo Card, thêm Action và random seed consistency thành công trên môi trường python  
- Nghiên cứu & Áp dụng paper [Domain Randomization for Transferring Deep Neural Networks from Simulation to the Real World (Tobin et al., 2017\)](https://arxiv.org/pdf/1703.06907):  
+ Ánh xạ bài toán:  
* Trong paper: Mục tiêu là train robot trong simulation để chạy tốt ngoài thực tế mà không cần train lại. Sự khác biệt giữa 2 môi trường gọi là reality gap. Giả thuyết paper đưa ra là nếu mô hình được huấn luyện trong một môi trường mô phỏng có độ biến thiên đủ lớn, thì thế giới thực sẽ chỉ xuất hiện đối với mô hình như là một biến thể khác của môi trường mô phỏng đó  
* Trong Đồ án này: Mục tiêu là train agent trên dữ liệu gốc để chơi tốt trên các phiên bản game đã cân bằng. Sự thay đổi chỉ số chính là reality gap.  
+ Phương pháp áp dụng:  
* Nguyên lý: Thay vì train agent học thuộc lòng một bộ chỉ số cố định (overfitting), ta biến môi trường training thành một tập hợp các biến thể ngẫu nhiên  
* Triển khai: Tại hàm reset(), áp dụng uniform noise lên các thông số game (HP, Damage, Mana Cost) trong khoảng biên độ mà thuật toán cân bằng sẽ tìm kiếm  
* Mục đích: Ép agent học logic hành vi thay vì học giá trị số. Ví dụ thay vì học "thẻ A damage 12 \-\> đánh", agent sẽ học "thẻ A damage \> máu địch \-\> đánh". Điều này giúp agent hoạt động ổn định trên Pareto Front mà không cần train lại  
- Nghiên cứu lý thuyết:  
+ Tài liệu tham khảo: Đã đọc và phân tích paper   
+ Vấn đề: Trong quá trình cân bằng game, các thông số (Damage, HP, Cost) thay đổi liên tục, agent thông thường sẽ bị overfit vào các con số cố định và chơi tệ khi game được chỉnh sửa  
+ Giải pháp dự định áp dụng: Triển khai kỹ thuật domain randomization vào môi trường Gym, thay vì train trên thông số cố định, môi trường sẽ random nhẹ các chỉ số trong hàm reset(): chỉ số base ± khoảng mà thuật toán cân bằng sẽ chỉnh sửa  
+ Kết quả: Ép agent học chiến thuật/logic (ví dụ khi chỉ số damage của thẻ bài strike là 12 thì nên chơi, còn khi là 3 thì nên ưu tiên các lá khác) thay vì học vẹt, giúp agent bền vững với các thay đổi cân bằng sau này

  3. ### Kế hoạch tuần tới

- Map toàn bộ logic từ C\# DLL vào hàm step() của python (thay thế mock logic hiện tại)  
- Viết script chuyển đổi dữ liệu cấu hình game từ json gốc sang cấu trúc runtime của python  
- Tìm hiểu các nghiên cứu liên quan đến tối ưu PPO agent cho game tương tự

# Tuần 2

5. ## Tuần 2: từ ngày 5/1 đến 11/1

   1. ### Mục tiêu tuần này

- Triển khai kết nối giữa môi trường game (C\#) và agent (python) phục vụ huấn luyện  
- Benchmark hiệu năng các phương thức giao tiếp (network vs local)

  2. ### Kết quả

- Thử nghiệm gRPC \+ Ngrok (Phương án ban đầu):  
+ Kết quả: Đã thiết lập thành công kết nối giữa kaggle và localhost  
+ Vấn đề phát sinh: Độ trễ mạng qua Ngrok quá cao (gần 25ms cho một phương thức GET data rỗng đơn giản như dưới hình), không đảm bảo tốc độ training hàng triệu steps 

  ![][image1]

- Tìm hiểu để chuẩn bị chuyển đổi sang giải pháp nhúng trực tiếp:  
+ Giải pháp mới: Build core game logic thành thư viện .dll (cross-platform .NET 9\) và nhúng trực tiếp vào python process sử dụng thư viện pythonnet  
+ Lợi ích: Loại bỏ hoàn toàn độ trễ mạng, tốc độ giao tiếp tương đương gọi hàm nội bộ  
- Vấn đề tương thích OS:  Kaggle chạy trên Linux container, trong khi file .dll thường build trên Windows, giải pháp em ghi trong phần kế hoạch bên dưới  
- Việc cài đặt pythonnet trên môi trường Linux của kaggle khá phức tạp và dễ lỗi config nên em đã tìm hiểu phương án dự phòng: Build game thành file thực thi binary (Linux executable) và giao tiếp qua stdin/stdout hoặc local socket (vẫn nhanh hơn nhiều so với qua Internet)

  3. ### Kế hoạch tuần tới

- Refactor code C\# để loại bỏ các dependency chỉ chạy trên Windows.  
- Cấu hình build target sang .NET 9 (Linux-x64)  
- Viết script cài đặt .NET runtime tự động trên Kaggle Notebook (apt-get install dotnet-sdk-9.0)  
- Xử lý dependency cho pythonnet trên Linux (cấu hình mono/env variables)  
- Hoàn thiện wrapper chuẩn OpenAI Gym (Gymnasium) bao quanh các hàm C\# đã import  
- Định nghĩa chính xác observation space và action space

# Tuần 1

6. ## Tuần 1: từ ngày 31/12 đến 4/1

   1. ### Mục tiêu tuần này

- Củng cố nền tảng lý thuyết về Học tăng cường (Reinforcement Learning \- RL) và Học tăng cường sâu (Deep Reinforcement Learning \- DRL)  
- Xác định thuật toán phù hợp để huấn luyện nhiều agent độc lập đại diện cho các lối chơi khác nhau (aggressive, defensive, …)  
- Xác định tính khả thi của việc áp dụng các thuật toán này vào kiến trúc hiện tại của dự án

  2. ### Tài liệu nghiên cứu

- [OpenAI Spinning Up](https://spinningup.openai.com/en/latest/index.html)  
- [DeepMind x UCL | Introduction to Reinforcement Learning 2015](https://youtube.com/playlist?list=PLqYmG7hTraZDM-OYHWgPebj2MfCFzFObQ&si=z3Xwjd-djf8xd49D)

  3. ### Kết quả

- Hệ thống hóa kiến thức: Đã nắm vững các khái niệm nền tảng: Markov Decision Process, value function (V, Q), Bellman equations  
- Phân tích thuật toán:  
+ Hiểu sâu về Proximal Policy Optimization (PPO) (Policy-based, On-policy). Nhận định được ưu điểm của PPO (tính ổn định, khả năng hỗ trợ hành động ngẫu nhiên/stochastic) so với DQN trong bối cảnh cân bằng game  
- Định hướng kỹ thuật:  
+ Loại bỏ các thuật toán khác không dành cho hành động rời rạc (DDPG, TD3), kém ổn định và học chậm (Vanilla Policy Gradient, A2C/A3C), phức tạp và nặng tính toán (TRPO)  
+ SAC thì cũng khá tốt và phù hợp, tuy nhiên lý do em loại bỏ nó như sau: logic game của em được viết hoàn toàn bằng C\# và khó để chuyển đổi sang thành python, nên em quyết định chạy logic game trên server và train agent trên kaggle, kết nối với nhau qua internet, và với mỗi agent em sẽ train song song trên vài đến vài chục game cunfg lúc. SAC thì cần chạy dựa trên replay buffer, việc truyền replay buffer qua lại sẽ khó và nặng hơn nhiều so với PPO chỉ truyền trajectory

  \=\> Xác định PPO là thuật toán phù hợp nhất cho dự án do khả năng hỗ trợ training song song và phù hợp với mô hình triển khai cloud training (Client-Server)

  4. ### Kế hoạch tuần tới

- Thiết lập hệ thống gRPC service rồi dùng ngrok để expose logic game lên public internet  
- Tạo một bot heuristic đơn giản trên Kaggle notebook  
- Kết nối về server để chạy thử  
- Benchmark tốc độ training qua mạng. Nếu quá chậm em sẽ chuyển game server sang cloud cùng region với notebook

[image1]: <data:image/png;base64,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>