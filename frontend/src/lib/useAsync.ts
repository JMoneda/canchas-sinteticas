import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../api/client';

export function errorMessage(e: unknown): string {
  if (e instanceof ApiError) {
    return e.message;
  }
  return 'No se pudo conectar con el servidor. ¿Está corriendo la API?';
}

interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  reload: () => void;
}

export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const memoFn = useCallback(fn, deps);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    memoFn()
      .then((result) => {
        if (active) {
          setData(result);
        }
      })
      .catch((e) => {
        if (active) {
          setError(errorMessage(e));
        }
      })
      .finally(() => {
        if (active) {
          setLoading(false);
        }
      });
    return () => {
      active = false;
    };
  }, [memoFn, tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);

  return { data, loading, error, reload };
}
