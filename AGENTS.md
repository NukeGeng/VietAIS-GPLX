# AGENTS.md — VietAIS GPLX

## 1. Quy tắc bắt buộc trước khi code

Agent PHẢI đọc theo thứ tự:

1. `GOAL.md`
2. `WORKFLOW.md`
3. `GITRULEBASE.md`
4. Convention/module hiện tại liên quan đến task

Không được bỏ qua bước này.

---

## 2. Source of truth

`GOAL.md` quyết định:

- Scope.
- Architecture.
- Module boundaries.
- Technology stack.
- Out-of-scope.

`WORKFLOW.md` quyết định:

- Cách thực hiện task.
- Cách verify.
- Cách review.
- Cách chuẩn bị PR.

`GITRULEBASE.md` quyết định:

- Branching.
- Commit.
- Draft PR.
- Merge.
- Branch cleanup.

Nếu instructions xung đột, dừng việc mở rộng scope và ưu tiên tài liệu có phạm vi trực tiếp nhất; không tự sáng tạo architecture mới.

---

## 3. Không Overengineering

Agent KHÔNG được tự thêm:

- Microservices.
- RabbitMQ.
- Kafka.
- Redis.
- Kubernetes.
- Distributed Saga.
- Generic Repository.
- BaseRepository.
- BaseService.
- Unused abstraction.
- Unused interface.
- Future-proof layer không có use case.
- Event Sourcing cho mọi entity.
- Async messaging chỉ vì “best practice”.
- New dependency chỉ để giảm vài dòng code.

Chỉ thêm khi:

1. GOAL yêu cầu; hoặc
2. Task hiện tại có requirement thực tế và thay đổi đã được chấp nhận trong GOAL.

---

## 4. Giữ đúng Modular Monolith

Không biến module thành microservice giả lập.

Không tạo network boundary giữa module trong cùng application.

Module giao tiếp thông qua contract/message rõ ràng khi cần.

Không tạo circular dependency.

Không truy cập trực tiếp internal implementation của module khác nếu đã có public contract phù hợp.

---

## 5. DDD / Event Storming

Với business command mới, agent phải xác định:

```text
Actor
Command
Aggregate
Invariant
Domain Event
Policy / Reaction
Projection / Read Model
Permission
```

Không tạo domain event chỉ để log CRUD.

Không để business invariant nằm rải rác ở controller/endpoint nếu nó thuộc Aggregate/Domain.

---

## 6. Marten

Dùng trực tiếp Marten session abstractions phù hợp.

Không bọc Marten bằng generic repository.

- Read: `IQuerySession`.
- Write: `IDocumentSession` hoặc integration phù hợp với Wolverine/Marten transaction.

Marten Document dùng cho current/reference state.

Event Sourcing chỉ dùng cho aggregate được GOAL chỉ định hoặc được GOAL cập nhật.

---

## 7. Snapshot

Snapshot dùng để tối ưu command-side aggregate state.

Không coi Snapshot là read model cho UI nếu Projection phù hợp hơn.

Không thêm Snapshot cho stream ngắn/chưa có nhu cầu chỉ vì Event Sourcing hỗ trợ nó.

---

## 8. Projection

Agent phải chọn projection mode theo requirement.

### Inline

Dùng khi cần consistency ngay và write cost chấp nhận được.

### Async

Dùng cho global aggregate, analytics, ranking, fan-out hoặc workload nặng.

Không chuyển toàn bộ projection sang Inline.

Không chuyển toàn bộ projection sang Async.

PR phải giải thích mode được chọn nếu thêm projection mới.

---

## 9. Wolverine

Wolverine dùng làm command/message backbone theo GOAL.

Ưu tiên local/in-process/durable mechanisms hiện có trước.

Không thêm external broker nếu chưa được yêu cầu.

Dùng Inbox/Outbox khi transaction/message delivery cần bảo đảm.

Không publish message không có consumer/use case thực tế.

---

## 10. Authorization

Không hard-code role name trong business logic.

Sai:

```csharp
if (role == "Admin")
```

Dùng permission/policy.

Không tự tạo thêm role ngoài GOAL nếu task không yêu cầu.

---

## 11. Data

Official data phải đi qua:

```text
source
→ normalized
→ validated
→ seed/import
```

Không lấy competitor website làm source of truth.

Không copy:

- Competitor explanations.
- Competitor database.
- Competitor watermark.
- Competitor proprietary images/assets.

Không hard-code question count, passing score, duration hoặc exam distribution nếu thuộc `ExamBlueprint`/`Regulation` versioned data.

---

## 12. Frontend

### 12.1 Frontend Design Source of Truth

Frontend reference bắt buộc nằm tại:

```text
/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas
```

Đây là **Frontend Design Source of Truth** cho VietAIS GPLX.

Trước MỌI task frontend, agent PHẢI:

1. Đọc `GOAL.md`, `WORKFLOW.md`, `GITRULEBASE.md`.
2. Xác định page/feature GPLX cần implement.
3. Inspect source frontend tại đường dẫn trên.
4. Tìm page/component/layout/pattern gần nhất với feature đang làm.
5. Ghi nhận visual rules cần tái sử dụng trước khi code.
6. Implement GPLX theo visual language của source reference.
7. So sánh lại implementation với reference trước khi coi task hoàn thành.

Reference frontend là **visual/interaction source of truth**, bao gồm:

- Layout.
- Grid.
- Spacing.
- Typography.
- Color usage.
- Border radius.
- Shadows.
- Card pattern.
- Button states.
- Input/form states.
- Header/navigation.
- Sidebar/drawer.
- Modal/dialog.
- Table/list pattern.
- Tabs.
- Badge/status.
- Empty state.
- Loading state.
- Error state.
- Responsive behavior.
- Interaction patterns.
- Motion/animation chỉ khi reference thực sự có và cần thiết.

Agent PHẢI ưu tiên reuse pattern có sẵn thay vì tự phát minh design mới.

### 12.2 Reference source là READ-ONLY

Agent KHÔNG được sửa, format, rename, xóa hoặc refactor bất kỳ file nào trong:

```text
/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas
```

Không commit source reference vào repository GPLX.

Không dùng reference source làm nơi implement GPLX.

### 12.3 Chỉ học Design, không copy Architecture/Business

Được phép học và áp dụng:

```text
UI / UX
Design language
Component appearance
Responsive behavior
Interaction pattern
Page composition
```

KHÔNG được tự động copy:

```text
Business logic
API contract
Backend assumptions
Domain model
Authentication flow
Smart Gas-specific state management
Smart Gas-specific routes
Smart Gas-specific naming
Unrelated dependencies
```

Quy tắc bắt buộc:

> Reference frontend quyết định **cách sản phẩm trông và tương tác**, còn `GOAL.md` quyết định **sản phẩm GPLX phải làm gì và kiến trúc ra sao**.

Nếu reference pattern xung đột với GOAL/business requirement, ưu tiên GOAL và giữ visual language gần nhất có thể.

### 12.4 Design consistency

Không redesign frontend mẫu nếu task không yêu cầu.

Không tự tạo design system khác.

Không tùy ý đổi:

- Font.
- Color palette.
- Radius system.
- Spacing convention.
- Component density.
- Navigation pattern.

Không thêm arbitrary gradient, glassmorphism, animation hoặc visual effect chỉ để "đẹp hơn" nếu reference không dùng.

Nếu cần component GPLX mà reference chưa có, agent phải:

1. Tìm pattern gần nhất trong reference.
2. Mở rộng pattern đó.
3. Giữ typography/spacing/color/state behavior nhất quán.
4. Không tạo một visual language mới.

### 12.5 Frontend component rule

Ưu tiên cấu trúc:

```text
reference visual pattern
→ shared UI component nếu thực sự reuse
→ GPLX business component
→ page/feature
```

Không abstract component chỉ vì "có thể dùng lại sau này".

Chỉ tạo shared component khi:

- Có ít nhất một use case hiện tại rõ ràng; hoặc
- Pattern chắc chắn xuất hiện ở nhiều màn hình đang nằm trong GOAL V1.

### 12.6 SEO/GEO và responsive

Public SEO/GEO pages phải giữ semantic HTML và crawlability.

Không biến mọi page thành client-only SPA nếu GOAL yêu cầu SSR/SSG.

Mọi frontend task phải kiểm tra tối thiểu:

- Desktop.
- Tablet.
- Mobile.
- Loading state.
- Empty state nếu có.
- Error state nếu có.
- Keyboard/focus behavior khi phù hợp.

Nếu local reference path không đọc được, agent phải báo rõ trong task/PR và **không tự redesign theo phỏng đoán**.

---

## 13. Performance

Không tối ưu giả định.

Ưu tiên:

1. Correct query pattern.
2. Read model phù hợp.
3. Proper indexing.
4. Nginx/public cache phù hợp.
5. Stateless backend.
6. Horizontal scaling.

Chỉ thêm distributed cache/broker sau khi có requirement hoặc bottleneck thực tế.

Không query full event stream cho public read nếu đã có projection phù hợp.

Không lưu timer tick thành event.

Không append event khi state thực tế không thay đổi.

---

## 14. Testing

Build pass không có nghĩa task hoàn thành.

Mọi task phải có verification tương xứng.

Không:

- Xóa test để pass pipeline.
- Skip test quan trọng mà không giải thích.
- Thay đổi behavior đúng chỉ để phù hợp test sai.

Khi sửa public contract phải cập nhật test/consumer liên quan.

---

## 15. Scope discipline

Không sửa ngoài scope task.

Không refactor module khác chỉ vì “tiện”.

Nếu thấy technical debt không liên quan:

- Ghi chú trong PR nếu cần.
- Không tự sửa trừ khi nó block task hiện tại.

---

## 16. Dependency discipline

Không thêm dependency nếu .NET/Nuxt/Marten/Wolverine hiện tại đã giải quyết được yêu cầu một cách rõ ràng.

Mọi dependency mới phải có lý do thực tế.

Không thêm library chỉ để tránh viết vài dòng code đơn giản.

---

## 17. Public contract discipline

Không thay endpoint route, request/response schema, event contract hoặc normalized data schema ngoài scope mà không cập nhật:

- Consumers.
- Tests.
- Documentation/schema tương ứng.

Không silently breaking change.

---

## 18. Completion rule

Agent chỉ được coi task hoàn thành khi:

- Scope đúng GOAL.
- Workflow đã đi qua verification.
- Test/build phù hợp pass.
- Không có thay đổi ngoài scope.
- Không có overengineering mới.
- Draft PR đủ thông tin để review.

Nếu build pass nhưng Definition of Done chưa đạt: task CHƯA hoàn thành.
