<script setup lang="ts">
import { demoQuestions } from "~/data/demo";

const route = useRoute();
const { request } = useGplxApi();
const attemptId = String(route.params.id);
const { data: attemptData, error: attemptError } = await useAsyncData(
  `exam-${attemptId}`,
  () => request<{ view: any }>(`/exams/${attemptId}`),
  { default: () => null },
);
const attempt = computed(() => attemptData.value?.view ?? attemptData.value);
const questionIds = computed(
  () => attempt.value?.questionIds ?? demoQuestions.map((item) => item.id),
);
const { data: questionData } = await useAsyncData(
  `exam-questions-${attemptId}`,
  async () => {
    if (!attempt.value?.questionIds?.length) return demoQuestions;
    return Promise.all(
      questionIds.value.map((id) => request<any>(`/questions/${id}`)),
    );
  },
  { default: () => demoQuestions },
);
const questions = computed(() => questionData.value ?? demoQuestions);
const currentIndex = ref(0);
const saving = ref(false);
const submitError = ref("");
const currentQuestion = computed(() => questions.value[currentIndex.value]);
const selectedOption = computed(
  () => attempt.value?.answers?.[currentQuestion.value?.id],
);
const answeredCount = computed(
  () => Object.keys(attempt.value?.answers ?? {}).length,
);

useSeoMeta({ title: "Thi thử GPLX — VietAIS", robots: "noindex,nofollow" });

async function chooseOption(optionId: string) {
  if (!currentQuestion.value || saving.value) return;
  saving.value = true;
  submitError.value = "";
  try {
    const result = await request<{ view: any }>(`/exams/${attemptId}/answers`, {
      method: "POST",
      body: { questionId: currentQuestion.value.id, optionId },
    });
    attemptData.value = result as any;
  } catch {
    submitError.value = "Không lưu được đáp án. Hãy thử lại.";
  } finally {
    saving.value = false;
  }
}

async function submitExam() {
  if (saving.value) return;
  saving.value = true;
  try {
    await request(`/exams/${attemptId}/submit`, { method: "POST" });
    await navigateTo(`/result/${attemptId}`);
  } catch {
    submitError.value = "Không thể nộp bài lúc này.";
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <section class="content-width exam-section">
    <div v-if="attemptError && !attempt" class="empty-state">
      <strong>Không tìm thấy bài thi</strong
      ><NuxtLink to="/">Về trang chủ</NuxtLink>
    </div>
    <template v-else>
      <div class="exam-topbar">
        <div>
          <span class="eyebrow"
            >Exam attempt / {{ attemptId.slice(0, 8) }}</span
          >
          <h1>
            Thi thử hạng {{ attempt?.licenseClassSlug?.toUpperCase() ?? "B" }}
          </h1>
        </div>
        <div class="exam-progress">
          <span>{{ answeredCount }}/{{ questions.length }} câu đã trả lời</span>
          <div class="progress-track">
            <i
              :style="{
                width: `${questions.length ? (answeredCount / questions.length) * 100 : 0}%`,
              }"
            />
          </div>
        </div>
      </div>
      <p v-if="submitError" class="inline-error" role="alert">
        {{ submitError }}
      </p>
      <div class="exam-layout">
        <aside class="exam-index surface-card">
          <span class="eyebrow">Tiến độ</span>
          <div class="index-grid">
            <button
              v-for="(question, index) in questions"
              :key="question.id"
              :class="{
                active: index === currentIndex,
                answered: attempt?.answers?.[question.id],
              }"
              @click="currentIndex = index"
            >
              {{ index + 1 }}
            </button>
          </div>
          <button
            class="button button-primary full-width"
            :disabled="saving"
            @click="submitExam"
          >
            {{ saving ? "Đang lưu…" : "Nộp bài" }}
          </button>
        </aside>
        <article v-if="currentQuestion" class="exam-question surface-card">
          <div class="detail-meta">
            <span>Câu {{ currentIndex + 1 }} / {{ questions.length }}</span
            ><span>{{ currentQuestion.topic }}</span>
          </div>
          <h2>{{ currentQuestion.text }}</h2>
          <div class="answer-list">
            <button
              v-for="option in currentQuestion.options"
              :key="option.id"
              :class="[
                'answer-option',
                { selected: selectedOption === option.id },
              ]"
              :disabled="saving"
              @click="chooseOption(option.id)"
            >
              <span>{{ option.id.toUpperCase() }}</span
              >{{ option.text }}<b v-if="selectedOption === option.id">✓</b>
            </button>
          </div>
          <div class="exam-navigation">
            <button
              class="button button-quiet"
              :disabled="currentIndex === 0"
              @click="currentIndex--"
            >
              ← Câu trước</button
            ><button
              class="button button-primary"
              :disabled="currentIndex === questions.length - 1"
              @click="currentIndex++"
            >
              Câu tiếp theo →
            </button>
          </div>
        </article>
      </div>
    </template>
  </section>
</template>
