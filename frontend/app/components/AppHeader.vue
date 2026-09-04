<template>
  <header class="site-header">
    <div class="content-width header-inner">
      <NuxtLink to="/" class="brand-mark" aria-label="VietAIS GPLX - Trang chủ">
        <span class="brand-symbol">G</span>
        <span class="brand-copy">
          <strong>VietAIS</strong>
          <small>GPLX</small>
        </span>
      </NuxtLink>
      <nav class="site-nav" aria-label="Điều hướng chính">
        <NuxtLink to="/questions">Ngân hàng câu hỏi</NuxtLink>
        <NuxtLink to="/licenses/b">Hạng B</NuxtLink>
        <NuxtLink to="/admin/login" class="nav-admin">Admin</NuxtLink>
      </nav>
    </div>
  </header>
</template>
