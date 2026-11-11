import { useCallback, useEffect, useState } from "react";
import { apiClient, errorMessage } from "./client";

export function useFetch<T>(path: string) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(() => {
    setError(null);
    apiClient
      .get<T>(path)
      .then((r) => setData(r.data))
      .catch((e) => setError(errorMessage(e)));
  }, [path]);

  useEffect(reload, [reload]);

  return { data, error, reload };
}

export async function act(run: () => Promise<unknown>, reload: () => void) {
  try {
    await run();
    reload();
  } catch (e) {
    alert(errorMessage(e));
  }
}
