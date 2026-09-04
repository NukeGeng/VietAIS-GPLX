<script setup lang="ts">
const route = useRoute();
const { request } = useGplxApi();
type PublicQuestion = {
  id: string;
  topic: string;
  text: string;
  licenseClassSlug: string;
  options: Array<{ id: string; text: string }>;
  explanation: string;
  memoryTip: string | null;
  source?: {
    title: string;
    url: string;
    effectiveFrom?: string;
    retrievedAt?: string;
  };
};
const { data: question, error: questionError } = await useAsyncData(
  `question-${route.params.id}`,
  () =>
    request<PublicQuestion>(`/questions/${route.params.id}`),
  {
    default: () => null,
  },
);
if (questionError.value) {
  const error = questionError.value as any;
  throw createError({
    statusCode: error.statusCode ?? 502,
    statusMessage: error.statusMessage ?? "Question API unavailable",
  });
}
if (!question.value) {
  throw createError({ statusCode: 404, statusMessage: "Question not found" });
}
const currentQuestion = computed(() => question.value);
useSeoMeta({
  title: () =>
    currentQuestion.value
      ? `${currentQuestion.value.text} — VietAIS GPLX`
      : "Câu hỏi GPLX — VietAIS",
  description: () =>
    currentQuestion.value?.explanation ?? "Giải thích câu hỏi lý thuyết GPLX.",
});
useGplxSeo("/questions/" + route.params.id, {
  "@type": "Question",
  name: currentQuestion.value?.text ?? "Câu hỏi GPLX",
  text: currentQuestion.value?.text ?? "Câu hỏi lý thuyết GPLX",
  learningResourceType: "Question",
  dateModified: currentQuestion.value?.source?.retrievedAt,
});
</script>

<template>
  <section
    v-if="currentQuestion"
    class="content-width page-section narrow-content"
  >
    <NuxtLink class="back-link" to="/questions"
      >← Quay lại ngân hàng câu hỏi</NuxtLink
    >
    <article class="question-detail surface-card">
      <div class="detail-meta">
        <span>{{ currentQuestion.topic }}</span
        ><span>Hạng {{ currentQuestion.licenseClassSlug.toUpperCase() }}</span>
      </div>
      <h1>{{ currentQuestion.text }}</h1>
      <ol class="option-list">
        <li v-for="option in currentQuestion.options" :key="option.id">
          <span>{{ option.id.toUpperCase() }}</span
          >{{ option.text }}
        </li>
      </ol>
      <div class="explanation-box">
        <span class="eyebrow">Giải thích</span>
        <p>{{ currentQuestion.explanation }}</p>
        <strong v-if="currentQuestion.memoryTip"
          >Mẹo nhớ: {{ currentQuestion.memoryTip }}</strong
        >
      </div>
      <div class="source-note">
        <span>Nguồn: {{ currentQuestion.source?.title }}</span>
        <span v-if="currentQuestion.source?.effectiveFrom"
          >Hiệu lực từ {{ currentQuestion.source.effectiveFrom }}</span
        >
        <span v-if="currentQuestion.source?.retrievedAt"
          >Cập nhật nguồn {{ currentQuestion.source.retrievedAt }}</span
        >
        <a
          v-if="currentQuestion.source?.url"
          :href="currentQuestion.source.url"
          target="_blank"
          rel="noreferrer"
          >Mở nguồn ↗</a
        >
      </div>
    </article>
  </section>
  <section v-else class="content-width page-section">
    <div class="empty-state">
      <strong>Không tìm thấy câu hỏi</strong
      ><NuxtLink to="/questions">Về ngân hàng câu hỏi</NuxtLink>
    </div>
  </section>
</template>
