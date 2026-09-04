<script setup lang="ts">
type PracticeQuestion = {
  id: string;
  topic: string;
  text: string;
  options: Array<{ id: string; text: string }>;
  correctOptionId: string;
  isCritical: boolean;
  explanation: string;
  memoryTip: string | null;
};

const route = useRoute();
const { request } = useGplxApi();
const licenseClassSlug = ref(String(route.query.licenseClassSlug ?? ""));
const topic = ref("");
const search = ref("");
const criticalOnly = ref(false);
const page = ref(1);
const pageSize = 10;
const selected = ref<Record<string, string>>({});

useSeoMeta({
  title: "Luyện tập câu hỏi GPLX — VietAIS",
  description: "Luyện tập câu hỏi GPLX theo hạng bằng, chủ đề và câu điểm liệt.",
});
useGplxSeo("/practice", {
  "@type": "LearningResource",
  name: "Luyện tập câu hỏi GPLX",
  description: "Luyện tập theo hạng bằng và chủ đề với giải thích, mẹo ghi nhớ.",
});

const { data: licenses } = await useAsyncData(
  "practice-licenses",
  () => request<Array<{ slug: string; code: string; name: string }>>("/licenses"),
  { default: () => [] },
);
const { data: topics } = await useAsyncData(
  "practice-topics",
  () => request<string[]>("/topics"),
  { default: () => [] },
);
const { data: response, pending } = await useAsyncData(
  "practice-questions",
  () => {
    const query = new URLSearchParams({ page: String(page.value), pageSize: String(pageSize) });
    if (licenseClassSlug.value) query.set("licenseClassSlug", licenseClassSlug.value);
    if (topic.value) query.set("topic", topic.value);
    if (search.value.trim()) query.set("search", search.value.trim());
    if (criticalOnly.value) query.set("critical", "true");
    return request<{ items: PracticeQuestion[]; total: number }>("/practice/questions?" + query);
  },
  { default: () => ({ items: [], total: 0 }), watch: [page, licenseClassSlug, topic, search, criticalOnly] },
);
const questions = computed(() => response.value?.items ?? []);
const total = computed(() => response.value?.total ?? 0);
const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize)));

watch([licenseClassSlug, topic, search, criticalOnly], () => {
  page.value = 1;
  selected.value = {};
});

function choose(question: PracticeQuestion, optionId: string) {
  selected.value[question.id] = optionId;
}
</script>

<template>
  <section class="content-width page-section">
    <div class="page-intro">
      <span class="eyebrow">Practice mode</span>
      <h1>Luyện tập theo trọng tâm</h1>
      <p>Chọn hạng bằng, chủ đề hoặc câu điểm liệt. Đáp án và giải thích chỉ hiện sau khi bạn chọn.</p>
    </div>
    <div class="practice-filters surface-card">
      <label><span>Hạng bằng</span><select v-model="licenseClassSlug"><option value="">Tất cả</option><option v-for="license in licenses" :key="license.slug" :value="license.slug">{{ license.code }} — {{ license.name }}</option></select></label>
      <label><span>Chủ đề</span><select v-model="topic"><option value="">Tất cả chủ đề</option><option v-for="item in topics" :key="item" :value="item">{{ item }}</option></select></label>
      <label><span>Tìm kiếm</span><input v-model="search" type="search" placeholder="Từ khóa…" /></label>
      <label class="checkbox-field"><input v-model="criticalOnly" type="checkbox" /><span>Chỉ câu điểm liệt</span></label>
    </div>
    <div v-if="pending" class="empty-state"><strong>Đang tải câu hỏi…</strong></div>
    <div v-else-if="!questions.length" class="empty-state"><strong>Không tìm thấy câu hỏi</strong><span>Thử bộ lọc khác.</span></div>
    <div v-else class="practice-list">
      <article v-for="(question, index) in questions" :key="question.id" class="practice-card surface-card">
        <div class="detail-meta"><span>Câu {{ (page - 1) * pageSize + index + 1 }}</span><span>{{ question.topic }}</span><span v-if="question.isCritical" class="critical-badge">Điểm liệt</span></div>
        <h2>{{ question.text }}</h2>
        <div class="practice-options">
          <button v-for="option in question.options" :key="option.id" :class="['practice-option', { selected: selected[question.id] === option.id, correct: selected[question.id] && option.id === question.correctOptionId, wrong: selected[question.id] === option.id && option.id !== question.correctOptionId }]" @click="choose(question, option.id)">
            <span>{{ option.id.toUpperCase() }}</span>{{ option.text }}<b v-if="selected[question.id] && option.id === question.correctOptionId">✓</b>
          </button>
        </div>
        <div v-if="selected[question.id]" class="explanation-box"><span class="eyebrow">Giải thích</span><p>{{ question.explanation }}</p><strong v-if="question.memoryTip">Mẹo nhớ: {{ question.memoryTip }}</strong></div>
      </article>
    </div>
    <div v-if="totalPages > 1" class="pagination" aria-label="Phân trang luyện tập"><button class="button button-quiet" :disabled="page === 1" @click="page--">← Trước</button><span>Trang {{ page }} / {{ totalPages }} · {{ total }} câu</span><button class="button button-quiet" :disabled="page === totalPages" @click="page++">Sau →</button></div>
  </section>
</template>
