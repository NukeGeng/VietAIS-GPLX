# GOAL.md — VietAIS GPLX

## 1. Mục tiêu dự án

VietAIS GPLX là nền tảng web ôn thi giấy phép lái xe tại Việt Nam, ưu tiên:

1. Dữ liệu đúng nguồn chính thức, có version và ngày hiệu lực rõ ràng.
2. Trải nghiệm học và thi thử tốt, nhanh, dễ dùng trên desktop và mobile web.
3. SEO + GEO được thiết kế từ kiến trúc ban đầu, không bổ sung kiểu vá về sau.
4. Backend có cấu trúc Modular Monolith rõ ràng, hướng Domain/Event-Driven bằng C# + Marten + Wolverine.
5. Event Sourcing chỉ áp dụng cho domain thực sự cần lịch sử hành vi; không Event Source toàn hệ thống.
6. Sử dụng hợp lý Snapshot, Inline Projection và Async Projection theo consistency requirement.
7. Hệ thống đủ tốt để scale theo chiều ngang khi traffic tăng mà không phải đổi sang Microservices sớm.
8. Chất lượng sản phẩm được ưu tiên hơn việc nhồi quảng cáo hoặc tối đa hóa doanh thu ngắn hạn.

---

## 2. Phạm vi V1

### 2.1 Guest

Guest không bắt buộc đăng nhập.

Guest có thể:

- Xem các hạng GPLX.
- Xem quy định thi đang áp dụng theo từng hạng.
- Xem ngân hàng câu hỏi.
- Tìm kiếm câu hỏi.
- Xem câu điểm liệt.
- Xem biển báo, sa hình, giải thích và mẹo ghi nhớ.
- Luyện tập theo hạng bằng.
- Luyện tập theo nhóm câu hỏi/chủ đề.
- Bắt đầu bài thi thử.
- Trả lời câu hỏi.
- Đổi đáp án.
- Đánh dấu/bỏ đánh dấu câu hỏi.
- Xem tiến độ bài thi.
- Nộp bài.
- Xem kết quả ngay sau khi nộp.
- Thi lại.
- Xem nội dung public phục vụ SEO/GEO.

### 2.2 Admin

Admin có thể:

- Đăng nhập Admin Portal.
- Import Question Bank.
- Validate dữ liệu import.
- Preview dữ liệu trước khi publish.
- Publish/deprecate Question Bank Version.
- Quản lý Question.
- Quản lý đáp án đúng.
- Quản lý câu điểm liệt.
- Quản lý giải thích/mẹo ghi nhớ.
- Quản lý License Class.
- Quản lý Regulation Version.
- Quản lý Exam Blueprint Version.
- Publish Regulation / Exam Blueprint.
- Xem analytics hệ thống.
- Xem trạng thái projection/async daemon.
- Thực hiện rebuild projection khi cần thiết và có quyền phù hợp.

---

## 3. Kiến trúc bắt buộc

### 3.1 Root structure

```text
root/
├── data/
├── frontend/
└── backend/
```

Không tự tạo thêm top-level application folder nếu chưa có lý do rõ ràng.

### 3.2 Backend stack

```text
ASP.NET Core
+ C#
+ PostgreSQL
+ Marten
+ Wolverine
+ Modular Monolith
+ DDD
+ Event-Driven
+ Event Sourcing có chọn lọc
+ Snapshot
+ Inline Projection
+ Async Projection
+ Marten Async Daemon
+ Wolverine Inbox/Outbox
```

### 3.3 Frontend stack

Frontend dùng Vue ecosystem, ưu tiên Nuxt để hỗ trợ:

- SSR/SSG.
- SEO.
- GEO.
- Sitemap.
- Dynamic metadata.
- Structured Data.
- Canonical URL.
- Public question pages có thể crawl độc lập.

Không chuyển framework nếu GOAL không được cập nhật.

#### Frontend Design Source of Truth

V1 phải học và áp dụng design từ frontend reference hiện có tại:

```text
/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas
```

Source này là **read-only reference** và được dùng làm chuẩn cho:

- Visual language.
- Layout.
- Typography.
- Colors.
- Spacing.
- Radius/shadow.
- Form controls.
- Navigation.
- Cards/tables/modals/tabs.
- Responsive behavior.
- Loading/empty/error states.
- Interaction patterns.

GPLX phải áp dụng design nhất quán theo reference, nhưng KHÔNG copy business logic, domain, API contract hoặc architecture của Smart Gas.

Nếu GPLX cần UI chưa tồn tại trong reference, mở rộng từ pattern gần nhất thay vì tự tạo style mới.

Không được sửa source reference và không commit source reference vào repository GPLX.

### 3.4 Server / Edge

Nginx chịu trách nhiệm:

- Reverse proxy.
- TLS termination.
- Static asset serving.
- Compression.
- HTTP/2 hoặc protocol phù hợp được hỗ trợ ổn định.
- Cache cho public/read-heavy endpoints phù hợp.
- Rate limiting cho endpoint cần bảo vệ.
- Connection management.
- Load balancing khi có nhiều backend instance.
- Security headers phù hợp.

---

## 4. Module boundaries

Backend tối thiểu gồm:

```text
backend/src/
├── Gplx.Api/
├── BuildingBlocks/
└── Modules/
    ├── QuestionBank/
    ├── Exams/
    ├── Learning/
    └── Identity/
```

### 4.1 QuestionBank

Quản lý reference data:

- Question.
- QuestionBankVersion.
- LicenseClass.
- Regulation.
- ExamBlueprint.

Mặc định lưu dưới dạng Marten Document.

Không Event Source Question chỉ để “đúng kiến trúc”.

### 4.2 Exams

Core domain của hành vi thi thử.

`ExamAttempt` là Event-Sourced Aggregate.

Core events:

- ExamStarted.
- QuestionAnswered.
- AnswerChanged.
- QuestionFlagged.
- QuestionUnflagged.
- ExamSubmitted.
- ExamScored.

### 4.3 Learning

Quản lý read models/analytics phục vụ:

- QuestionPerformance.
- DifficultQuestionRanking.
- Topic performance.
- Global pass rate.
- Learning analytics.
- Readiness/recommendation data sau này nếu cần.

### 4.4 Identity

V1 chỉ cần authentication/authorization cho Admin.

Guest không bắt buộc đăng nhập.

Không xây hệ thống account/social login phức tạp nếu GOAL chưa yêu cầu.

---

## 5. Event Storming baseline

Mọi feature mới phải được map tối thiểu theo chuỗi:

```text
Actor
→ Command
→ Aggregate / Domain rule
→ Domain Event
→ Policy / Reaction nếu có
→ Projection / Read Model
```

### 5.1 Question Bank flow

```text
Admin
→ ImportQuestionBank
→ QuestionBankVersion
→ QuestionBankImported
→ ValidateQuestionBank
→ QuestionBankValidated / QuestionBankValidationFailed
→ PublishQuestionBank
→ QuestionBankPublished
```

### 5.2 Exam flow

```text
Guest
→ StartExam
→ ExamAttempt
→ ExamStarted

Guest
→ AnswerQuestion
→ ExamAttempt
→ QuestionAnswered

Guest
→ ChangeAnswer
→ ExamAttempt
→ AnswerChanged

Guest
→ SubmitExam
→ ExamAttempt
→ ExamSubmitted
→ ExamScored
```

---

## 6. Versioning là bắt buộc

Không hard-code cấu trúc đề thi trong business code.

Phải model hóa:

- QuestionBankVersion.
- ExamBlueprintVersion.
- RegulationVersion.

Mỗi `ExamAttempt` khi bắt đầu phải pin:

- QuestionBankVersion.
- ExamBlueprintVersion.
- Regulation/Effective version cần thiết.

Admin thay đổi dữ liệu sau đó không được làm thay đổi bài thi đang diễn ra.

---

## 7. Snapshot / Projection strategy

### 7.1 Snapshot

Snapshot phục vụ command-side state và tối ưu aggregate load.

Không dùng Snapshot như một API read model thay thế projection.

### 7.2 Inline Projection

Chỉ dùng khi frontend/business cần dữ liệu cập nhật ngay trong transaction chính.

Ví dụ phù hợp:

- ExamAttemptSnapshot.
- ExamAttemptView.
- ExamResultView.

### 7.3 Async Projection

Dùng cho dữ liệu tổng hợp nặng hoặc eventual consistency chấp nhận được.

Ví dụ:

- QuestionPerformance.
- DifficultQuestionRanking.
- GlobalPassRate.
- DailyPlatformStatistics.
- LearningAnalytics.

Không ép tất cả projection về Inline hoặc Async.

Projection mode phải được quyết định theo:

1. Consistency requirement.
2. Write cost.
3. Query pattern.
4. Concurrency impact.

---

## 8. Wolverine

Wolverine là command/message backbone.

Dùng cho:

- Commands.
- Handlers.
- Domain/integration message processing khi phù hợp.
- Transactional Inbox/Outbox.
- Durable local queues khi cần.

V1 không yêu cầu RabbitMQ/Kafka.

Chỉ thêm external broker khi có use case thực tế và GOAL được cập nhật.

---

## 9. Data source

Nguồn dữ liệu ưu tiên:

1. Cục CSGT / Bộ Công an.
2. Cổng thông tin Chính phủ / Công báo để kiểm chứng văn bản, ngày hiệu lực.
3. Các nguồn khác chỉ dùng để tham khảo, không làm source of truth.

Không crawl/copy database, explanation, image, watermark hoặc UI asset từ website đối thủ để làm dữ liệu chính.

### 9.1 Data structure

```text
data/
├── sources/
│   ├── csgt/
│   └── regulations/
├── normalized/
│   ├── question-banks/
│   ├── license-classes/
│   ├── exam-blueprints/
│   └── regulations/
├── assets/
│   ├── questions/
│   ├── traffic-signs/
│   └── scenarios/
└── scripts/
    ├── extract/
    ├── normalize/
    ├── validate/
    └── seed/
```

Luồng bắt buộc:

```text
Official source
→ Raw source
→ Normalize
→ Validate
→ Manual QA khi cần
→ Seed/Import
→ Marten
```

Không import trực tiếp dữ liệu chưa normalize vào production database.

---

## 10. Authorization

Không hard-code role name trong business logic.

Sai:

```csharp
if (user.Role == "Admin")
```

Dùng permission/policy.

Ví dụ:

```text
questionbank.read
questionbank.import
questionbank.edit
questionbank.publish
regulation.read
regulation.manage
regulation.publish
exam-blueprint.read
exam-blueprint.manage
exam-blueprint.publish
analytics.read
system.projection.read
system.projection.rebuild
```

`Admin` là role gom các permission cần thiết.

Public Guest endpoints không yêu cầu permission claim.

---

## 11. Performance / scale requirements

Hệ thống phải giữ backend stateless theo request.

Không giữ Marten session trong suốt thời gian một bài thi.

Mỗi request:

```text
Open session
→ Execute
→ Commit nếu cần
→ Dispose
```

Không lưu timer tick thành event.

Timer dựa vào `StartedAt` / `ExpiresAt`.

Không tạo event nếu đáp án mới giống đáp án hiện tại.

Public/read-heavy data phải có chiến lược cache phù hợp.

Không query event stream cho homepage/listing/public read nếu đã có projection/read model phù hợp.

Thiết kế backend phải cho phép chạy nhiều instance phía sau Nginx mà không sửa business logic.

---

## 12. SEO + GEO foundation

V1 phải hỗ trợ:

- SSR/SSG phù hợp.
- Semantic HTML.
- Canonical URL.
- Sitemap.
- robots.txt.
- Dynamic title/description.
- Structured Data phù hợp.
- Question entity pages.
- License class pages.
- Regulation pages.
- Source provenance.
- Effective date.
- Last updated date.
- Internal linking rõ ràng.
- OAI-SearchBot/Bing/Google crawlability theo chính sách project.

Không tạo hàng loạt trang chỉ đổi keyword mà không có giá trị thật.

---

## 13. Out of Scope V1

Không tự triển khai các mục sau nếu GOAL chưa được cập nhật:

- Microservices.
- RabbitMQ.
- Kafka.
- Kubernetes.
- Distributed Saga.
- Redis chỉ vì “có thể cần”.
- AI chatbot.
- AI recommendation engine hoàn chỉnh.
- Mobile app.
- Social features.
- Complex user profile.
- Subscription/payment system.
- Premium membership.
- AdMob/mobile ads.
- Event Sourcing cho mọi entity.
- Generic Repository bọc Marten.
- BaseService/BaseRepository abstraction không có use case thực tế.
- CQRS tách thành hai database chỉ để đúng lý thuyết.

---

## 14. Definition of Done V1

Một V1 được xem là hoàn thành khi:

1. Official data được normalize, validate và seed thành công.
2. Guest có thể chọn hạng bằng và thi thử end-to-end.
3. ExamAttempt hoạt động bằng Event Sourcing.
4. Snapshot hoạt động đúng mục đích command-side.
5. Inline projection trả kết quả cần consistency ngay lập tức.
6. Async projection chạy được qua Marten Async Daemon cho analytics phù hợp.
7. Wolverine xử lý commands/messages và transactional behavior đúng thiết kế.
8. Admin có thể quản lý/publish Question Bank, Regulation và Exam Blueprint theo version.
9. Authorization không hard-code role trong business logic.
10. SEO/GEO foundation được triển khai.
11. Nginx được cấu hình cho reverse proxy/cache/security/performance cơ bản.
12. Backend có thể chạy nhiều instance stateless phía sau load balancer.
13. Build/test pass.
14. Critical flow có automated tests.
15. Không có feature ngoài GOAL được thêm chỉ để “chuẩn bị cho tương lai”.
