/**
 * A simple store to signal when data related to a specific API endpoint
 * needs to be refreshed across different components, written in Composition API style.
 */
export const useRefreshStore = defineStore('refresh', () => {
  /**
   * Stores a counter for each endpoint slug.
   * When a component needs to signal a refresh, it increments the counter.
   * Other components can watch this counter to react to the change.
   */
  const refreshCounters = ref<Record<string, number>>({});

  /**
   * Triggers a refresh for a specific endpoint by incrementing its counter.
   * @param endpoint The API endpoint slug (e.g., 'strains').
   */
  function triggerRefresh(endpoint: string) {
    if (refreshCounters.value[endpoint] === undefined) {
      refreshCounters.value[endpoint] = 0;
    }
    refreshCounters.value[endpoint]++;
  }

  return {
    refreshCounters,
    triggerRefresh,
  };
});
