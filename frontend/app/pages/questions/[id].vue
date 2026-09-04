<script setup lang="ts">
import { demoQuestions } from "~/data/demo";

const route = useRoute();
const { request } = useGplxApi();
const { data: question } = await useAsyncData(
  `question-${route.params.id}`,
  () =>
    request<(typeof demoQuestions)[number]>(`/questions/${route.params.id}`),
  {
    default: () =>
      demoQuestions.find((item) => item.id === route.params.id) ?? null,
  },
);
const currentQuestion = computed(
  () =>
    question.value ?? demoQuestions.find((item) => item.id === route.params.id),
);
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
