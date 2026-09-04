<script setup lang="ts">
import { demoQuestions } from "~/data/demo";

useSeoMeta({
  title: "Ngân hàng câu hỏi GPLX — VietAIS",
  description: "Tìm kiếm và ôn luyện câu hỏi lý thuyết GPLX theo chủ đề.",
});
useGplxSeo("/questions", {
  "@type": "CollectionPage",
  name: "Ngân hàng câu hỏi GPLX",
  description: "Tìm kiếm và ôn luyện câu hỏi lý thuyết GPLX theo chủ đề.",
});
const { request } = useGplxApi();
const search = ref("");
const page = ref(1);
const pageSize = 50;
const { data: response } = await useAsyncData(
  "questions",
  () => {
    const query = new URLSearchParams({
      page: String(page.value),
      pageSize: String(pageSize),
    });
    if (search.value.trim()) query.set("search", search.value.trim());
    return request<{
      items: typeof demoQuestions;
      page: number;
      pageSize: number;
      total: number;
    }>("/questions?" + query.toString());
  },
  {
    default: () => ({
      items: demoQuestions,
      page: 1,
      pageSize: demoQuestions.length,
      total: demoQuestions.length,
    }),
    watch: [page, search],
  },
);
const questions = computed(() => response.value?.items ?? demoQuestions);
const total = computed(() => response.value?.total ?? questions.value.length);
const totalPages = computed(() =>
  Math.max(1, Math.ceil(total.value / pageSize)),
);
watch(search, () => {
  page.value = 1;
});
</script>

<template>
  <section class="content-width page-section">
    <div class="page-intro">
      <span class="eyebrow">Question bank</span>
      <h1>Ngân hàng câu hỏi</h1>
      <p>
        Tìm câu hỏi theo nội dung hoặc chủ đề. Mỗi câu đều hiển thị nguồn và
        phiên bản dữ liệu.
      </p>
    </div>
    <label class="search-field"
      ><span aria-hidden="true">⌕</span
      ><input
        v-model="search"
        type="search"
        placeholder="Tìm kiếm câu hỏi…"
        aria-label="Tìm kiếm câu hỏi"
    /></label>
    <div class="question-list">
      <NuxtLink
        v-for="(question, index) in questions"
        :key="question.id"
        :to="`/questions/${question.id}`"
        class="question-row"
      >
        <span class="question-index">{{
          String(index + 1).padStart(2, "0")
        }}</span
        ><span class="question-topic">{{ question.topic }}</span
        ><span class="question-text">{{ question.text }}</span
        ><span class="row-arrow">→</span>
      </NuxtLink>
    </div>
    <div v-if="!questions.length" class="empty-state">
      <strong>Không tìm thấy câu hỏi</strong><span>Thử một từ khóa khác.</span>
    </div>
    <div v-if="totalPages > 1" class="pagination" aria-label="Phân trang">
      <button class="button button-quiet" :disabled="page === 1" @click="page--">
        ← Trước
      </button>
      <span>Trang {{ page }} / {{ totalPages }} · {{ total }} câu</span>
      <button
        class="button button-quiet"
        :disabled="page === totalPages"
        @click="page++"
      >
        Sau →
      </button>
    </div>
  </section>
</template>
