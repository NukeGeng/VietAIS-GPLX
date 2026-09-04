<script setup lang="ts">
const { request } = useGplxApi();
const email = ref("");
const password = ref("");
const error = ref("");
const loading = ref(false);
useSeoMeta({
  title: "Admin Portal — VietAIS GPLX",
  robots: "noindex,nofollow",
});
async function login() {
  loading.value = true;
  error.value = "";
  try {
    const result = await request<{ accessToken: string }>("/admin/auth/login", {
      method: "POST",
      body: { email: email.value, password: password.value },
    });
    localStorage.setItem("gplx_admin_token", result.accessToken);
    await navigateTo("/admin");
  } catch {
    error.value = "Email hoặc mật khẩu không đúng.";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <section class="content-width auth-section">
    <form class="auth-card surface-card" @submit.prevent="login">
      <span class="brand-symbol large">G</span
      ><span class="eyebrow">Admin portal</span>
      <h1>Đăng nhập quản trị</h1>
      <label
        >Email<input
          v-model="email"
          type="email"
          required
          autocomplete="username" /></label
      ><label
        >Mật khẩu<input
          v-model="password"
          type="password"
          required
          autocomplete="current-password"
      /></label>
      <p v-if="error" class="inline-error" role="alert">{{ error }}</p>
      <button class="button button-primary full-width" :disabled="loading">
        {{ loading ? "Đang xác thực…" : "Đăng nhập" }}
      </button>
    </form>
  </section>
</template>
