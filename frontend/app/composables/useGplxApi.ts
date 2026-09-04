export function useGplxApi() {
  const config = useRuntimeConfig();

  function request<T>(path: string, options: Record<string, unknown> = {}) {
    return $fetch<T>(path, {
      baseURL: config.public.apiBase,
      ...options,
    });
  }

  return { request };
}
