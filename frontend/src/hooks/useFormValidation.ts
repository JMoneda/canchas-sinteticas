import { useCallback, useMemo, useState } from 'react';

type Validator<TValues> = (value: string, allValues: TValues) => string | null;

type Validators<TValues> = {
  [K in keyof TValues]?: Validator<TValues>;
};

interface UseFormValidationResult<TValues> {
  values: TValues;
  errors: Partial<Record<keyof TValues, string>>;
  touched: Partial<Record<keyof TValues, boolean>>;
  isValid: boolean;
  setValue: (name: keyof TValues, value: string) => void;
  setTouched: (name: keyof TValues) => void;
  reset: () => void;
  /** Marca todo como tocado y devuelve si el formulario es válido. */
  validateAll: () => boolean;
}

/**
 * Hook genérico de validación de formularios: gestiona valores, campos tocados,
 * errores derivados de validadores puros y una bandera `isValid`. Los errores
 * se muestran solo cuando el campo fue tocado o tras `validateAll()`.
 */
export function useFormValidation<TValues extends Record<string, string>>(config: {
  initial: TValues;
  validators: Validators<TValues>;
}): UseFormValidationResult<TValues> {
  const { initial, validators } = config;
  const [values, setValues] = useState<TValues>(initial);
  const [touchedState, setTouchedState] = useState<Partial<Record<keyof TValues, boolean>>>({});

  // Errores "crudos" para TODOS los campos (independiente de touched).
  const rawErrors = useMemo(() => {
    const result: Partial<Record<keyof TValues, string>> = {};
    (Object.keys(validators) as (keyof TValues)[]).forEach((key) => {
      const validator = validators[key];
      if (validator) {
        const message = validator(values[key] ?? '', values);
        if (message) {
          result[key] = message;
        }
      }
    });
    return result;
  }, [values, validators]);

  const isValid = useMemo(() => Object.keys(rawErrors).length === 0, [rawErrors]);

  // Errores "mostrables": solo para campos tocados.
  const errors = useMemo(() => {
    const result: Partial<Record<keyof TValues, string>> = {};
    (Object.keys(rawErrors) as (keyof TValues)[]).forEach((key) => {
      if (touchedState[key]) {
        result[key] = rawErrors[key];
      }
    });
    return result;
  }, [rawErrors, touchedState]);

  const setValue = useCallback((name: keyof TValues, value: string) => {
    setValues((prev) => ({ ...prev, [name]: value }));
  }, []);

  const setTouched = useCallback((name: keyof TValues) => {
    setTouchedState((prev) => ({ ...prev, [name]: true }));
  }, []);

  const reset = useCallback(() => {
    setValues(initial);
    setTouchedState({});
  }, [initial]);

  const validateAll = useCallback(() => {
    const allTouched: Partial<Record<keyof TValues, boolean>> = {};
    (Object.keys(initial) as (keyof TValues)[]).forEach((key) => {
      allTouched[key] = true;
    });
    setTouchedState(allTouched);
    return Object.keys(rawErrors).length === 0;
  }, [initial, rawErrors]);

  return { values, errors, touched: touchedState, isValid, setValue, setTouched, reset, validateAll };
}
