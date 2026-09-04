# WORKFLOW.md — VietAIS GPLX

## 1. Nguyên tắc làm việc

Mọi công việc phải tuân theo thứ tự:

```text
GOAL.md
→ WORKFLOW.md
→ Module scope
→ Event Storming
→ Implementation
→ Test/Verify
→ Draft PR
→ Review
→ Submit PR
→ Cleanup branch
```

`GOAL.md` là nguồn quyết định scope.

Nếu một feature/architecture/dependency không có trong GOAL:

- Không tự triển khai.
- Không “chuẩn bị sẵn cho tương lai”.
- Không mở rộng scope vì thấy có thể hữu ích.

Nếu thực sự cần thay đổi kiến trúc hoặc scope, cập nhật GOAL trước rồi mới code.

---

## 2. Bước 1 — Chọn đúng module

Mỗi task phải thuộc rõ một khu vực:

```text
data
frontend
backend
```

Và nếu thuộc backend thì phải xác định module:

```text
QuestionBank
Exams
Learning
Identity
```

Không sửa module khác nếu không cần thiết cho task hiện tại.

### Nếu task thuộc frontend

Trước khi thiết kế hoặc code UI, bắt buộc inspect:

```text
/Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas
```

Thứ tự frontend task:

```text
GOAL requirement
→ xác định page/feature GPLX
→ inspect reference frontend
→ xác định layout/component/pattern tương ứng
→ implement GPLX
→ compare với reference
→ responsive/SEO/GEO verification
```

Không bắt đầu frontend bằng việc tự thiết kế UI mới nếu reference đã có pattern tương đương.

Reference chỉ quyết định design/interaction; không quyết định GPLX business logic hoặc architecture.

---

## 3. Bước 2 — Xác định Event Storming trước khi code business flow

Với command/business feature mới phải xác định:

```text
Actor
Command
Aggregate
Business Rule / Invariant
Domain Event
Policy / Reaction
Projection / Read Model
Permission
```

Ví dụ:

```text
Guest
→ SubmitExam
→ ExamAttempt
→ validate InProgress
→ ExamSubmitted
→ ExamScored
→ ExamResultView (Inline)
→ QuestionPerformance (Async)
```

Không bắt đầu bằng CRUD controller rồi mới nghĩ domain sau.

---

## 4. Bước 3 — Chọn đúng persistence pattern

Trước khi implement phải quyết định:

### Marten Document

Dùng cho reference/current-state data khi không cần event history làm source of truth.

Ví dụ:

- Question.
- QuestionBankVersion.
- LicenseClass.
- Regulation.
- ExamBlueprint.

### Event Sourcing

Chỉ dùng nếu lịch sử thay đổi/hành vi là một phần có giá trị của domain.

V1:

- ExamAttempt.

Không Event Source entity khác nếu GOAL chưa yêu cầu.

---

## 5. Bước 4 — Chọn Projection mode có chủ đích

Không mặc định Inline.

Không mặc định Async.

### Inline khi:

- User cần thấy kết quả ngay.
- Read model nằm trên critical flow.
- Update cost nhỏ và predictable.

### Async khi:

- Global aggregation.
- Analytics.
- Ranking.
- Expensive fan-out.
- Eventual consistency chấp nhận được.

Mọi projection mới phải ghi rõ lý do chọn Inline hoặc Async trong PR description.

---

## 6. Bước 5 — Implement Vertical Slice trong module

Feature nên nằm gần nhau:

```text
Features/
└── SubmitExam/
    ├── SubmitExamCommand.cs
    ├── SubmitExamHandler.cs
    ├── SubmitExamValidator.cs   # nếu cần
    ├── SubmitExamEndpoint.cs
    └── SubmitExamResponse.cs
```

Không tách file theo layer chỉ để đạt “clean architecture hình thức”.

Domain rules vẫn nằm trong Domain/Aggregate phù hợp.

---

## 7. Bước 6 — Test / Verification

Build success không đồng nghĩa task hoàn thành.

Mỗi task phải có verification phù hợp:

- Unit test cho business rules.
- Integration test cho Marten/Wolverine nếu feature phụ thuộc persistence/message behavior.
- Architecture test cho boundary quan trọng.
- API test/smoke test cho critical endpoint.
- Data validation test cho import/seed.
- Manual UI verification cho frontend behavior và đối chiếu với Frontend Design Source of Truth.

Không xóa test để pipeline xanh.

Không đổi expected behavior chỉ để test pass.

---

## 8. Bước 7 — Performance check theo loại feature

### Public read feature

Kiểm tra:

- Có query read model/document trực tiếp không?
- Có query event stream không cần thiết không?
- Có cache được ở Nginx không?
- Response có user-specific không?

### Write/event feature

Kiểm tra:

- Event có thật sự cần tạo không?
- Aggregate stream có concurrency protection không?
- Inline projection có quá nặng không?
- Có thể chuyển phần thống kê sang Async không?

Không tối ưu bằng cách thêm Redis/RabbitMQ trước khi có bottleneck hoặc requirement rõ.

---

## 9. Bước 8 — SEO/GEO check cho public page

Mỗi public route quan trọng phải kiểm tra nếu phù hợp:

- SSR/SSG.
- Title.
- Description.
- Canonical.
- Structured data.
- Semantic HTML.
- Source provenance.
- Effective date / last updated.
- Internal links.
- Sitemap eligibility.

Không generate spam pages chỉ để bắt keyword.

### Frontend Definition of Done

Nếu PR có frontend change, phải verify:

```text
[ ] Đã inspect /Users/dangvyhao/Documents/NLT_Tranning/front-end/nlt-smart-gas
[ ] Không sửa reference source
[ ] Layout bám reference pattern
[ ] Typography bám reference
[ ] Color/spacing/radius bám reference
[ ] Component states nhất quán
[ ] Không tự tạo design system mới
[ ] Không copy Smart Gas business logic/API
[ ] Desktop OK
[ ] Tablet OK
[ ] Mobile OK
[ ] Loading/empty/error state phù hợp
[ ] SEO/GEO semantics không bị phá
[ ] Không thêm UI dependency không cần thiết
```

Nếu reference không có component tương ứng, PR phải nêu pattern gần nhất đã dùng để mở rộng.

---

## 10. Bước 9 — Draft PR

Khi hoàn thành một module/task có ý nghĩa:

```text
feature branch
→ Draft PR vào develop
```

Draft PR phải có tối thiểu:

```text
## Scope
Task này làm gì.

## Event Storming
Actor → Command → Aggregate → Event → Projection.

## Architecture
Document/Event Sourcing/Snapshot/Projection mode nào được dùng và tại sao.

## Verification
Test/build/manual verification đã chạy.

## Frontend Reference (nếu có FE change)
Reference page/component/pattern nào đã được áp dụng từ nlt-smart-gas.
Xác nhận reference source không bị thay đổi.

## Out of Scope
Những gì chủ động không làm.

## Risks / Follow-up
Chỉ nêu rủi ro thực tế, không mở thêm scope.
```

---

## 11. Bước 10 — Review Draft PR

Trước khi submit:

1. Review diff toàn bộ.
2. Kiểm tra có file ngoài scope không.
3. Kiểm tra dependency mới có cần thiết không.
4. Kiểm tra có abstraction thừa không.
5. Kiểm tra có hard-code role không.
6. Kiểm tra public contract có bị thay đổi ngoài ý muốn không.
7. Kiểm tra projection mode đúng requirement không.
8. Kiểm tra data/versioning có bị hard-code không.
9. Chạy test/build lại.
10. Resolve review comments.

Sau khi review xong mới chuyển Draft PR thành Ready/Submit PR vào `develop`.

---

## 12. Bước 11 — Sau khi merge

Sau khi PR merge vào `develop`:

- Sync local `develop`.
- Xóa branch đã merge nếu không còn dùng.
- Xóa remote branch nếu policy repo cho phép.
- Không giữ branch cũ chỉ vì “có thể dùng lại”.
- Branch lỗi thời hoặc diverge quá xa phải được dọn.

---

## 13. Quy tắc chống Overengineering

Trước khi thêm bất kỳ thứ gì, hỏi:

1. GOAL hiện tại có yêu cầu không?
2. Use case hiện tại có dùng không?
3. Có giải quyết bottleneck/bug/requirement thực tế không?
4. Có cách đơn giản hơn không?

Nếu câu trả lời chủ yếu là:

> “sau này có thể cần”

thì mặc định **không làm**.

---

## 14. Thứ tự ưu tiên khi có xung đột

```text
Correctness
> Data integrity
> Security
> Product UX
> Maintainability
> Performance theo bottleneck thực tế
> Developer convenience
> Architectural purity
```

Không hy sinh product correctness chỉ để kiến trúc trông đẹp hơn.
