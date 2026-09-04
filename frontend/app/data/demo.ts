export const demoLicenses = [
  {
    slug: "b",
    code: "B",
    name: "Hạng B",
    description:
      "Luyện tập lý thuyết và thi thử theo bộ đề đã được phiên bản hóa.",
  },
  {
    slug: "c1",
    code: "C1",
    name: "Hạng C1",
    description:
      "Không gian ôn tập theo nhóm chủ đề, có tiến độ và kết quả rõ ràng.",
  },
];

export const demoQuestions = [
  {
    id: "00000000-0000-0000-0000-000000000001",
    slug: "development-q-001",
    licenseClassSlug: "b",
    topic: "Quy tắc giao thông",
    text: "Khi chuẩn bị chuyển hướng, người điều khiển xe cần thực hiện thao tác nào trước?",
    options: [
      { id: "a", text: "Quan sát, giảm tốc độ và báo hướng chuyển động" },
      { id: "b", text: "Tăng tốc độ để hoàn thành nhanh" },
      { id: "c", text: "Chỉ bật đèn sau khi đã chuyển hướng" },
      { id: "d", text: "Không cần quan sát nếu đường vắng" },
    ],
    explanation:
      "Cần quan sát, bảo đảm an toàn, giảm tốc độ phù hợp và báo hướng chuyển động trước khi chuyển hướng.",
    memoryTip: "Quan sát — giảm tốc — báo hướng.",
  },
  {
    id: "00000000-0000-0000-0000-000000000002",
    slug: "development-q-002",
    licenseClassSlug: "b",
    topic: "Biển báo",
    text: "Khi gặp biển báo giao thông, người lái xe nên ưu tiên thực hiện điều gì?",
    options: [
      {
        id: "a",
        text: "Tuân thủ nội dung báo hiệu và điều chỉnh hành vi lái xe",
      },
      { id: "b", text: "Chỉ quan tâm biển báo có màu đỏ" },
      { id: "c", text: "Bỏ qua nếu chưa từng gặp biển báo" },
      { id: "d", text: "Dừng xe giữa làn đường để đọc lâu hơn" },
    ],
    explanation:
      "Biển báo là một phần của hệ thống báo hiệu đường bộ và cần được chấp hành theo tình huống thực tế.",
    memoryTip: "Đọc đúng — làm đúng — an toàn trước.",
  },
  {
    id: "00000000-0000-0000-0000-000000000003",
    slug: "development-q-003",
    licenseClassSlug: "b",
    topic: "Văn hóa giao thông",
    text: "Trong tình huống giao thông phức tạp, nguyên tắc ưu tiên nào nên được đặt lên trước?",
    options: [
      { id: "a", text: "An toàn của người và phương tiện" },
      { id: "b", text: "Tốc độ hoàn thành hành trình" },
      { id: "c", text: "Quyền đi trước của xe lớn" },
      { id: "d", text: "Bấm còi liên tục" },
    ],
    explanation:
      "Mọi quyết định điều khiển phương tiện cần ưu tiên an toàn và tuân thủ báo hiệu, quy tắc giao thông.",
    memoryTip: "An toàn trước, nhanh sau.",
  },
];
