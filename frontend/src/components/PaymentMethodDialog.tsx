import { useState } from 'react';
import { paymentMethodOptions, formatCurrency } from '../lib/format';
import { Button, ModalShell } from './ui';

/**
 * Diálogo para elegir el método de pago (Nequi, PSE, Bancolombia, tarjeta) antes de iniciar
 * el checkout del proveedor.
 */
export function PaymentMethodDialog({
  amount,
  busy,
  onConfirm,
  onClose,
}: {
  amount: number;
  busy: boolean;
  onConfirm: (method: string) => void;
  onClose: () => void;
}) {
  const [method, setMethod] = useState(paymentMethodOptions[0].value);

  return (
    <ModalShell size="md" onClose={onClose}>
      <h2 className="text-lg font-semibold text-slate-800">Elige cómo pagar</h2>
      <p className="mt-1 text-sm text-slate-500">
        Total a pagar: <span className="font-semibold text-slate-800">{formatCurrency(amount)}</span>
      </p>

      <div className="mt-4 space-y-2">
        {paymentMethodOptions.map((opt) => (
          <label
            key={opt.value}
            className={`flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2.5 text-sm ${
              method === opt.value
                ? 'border-brand-500 bg-brand-50 text-brand-800'
                : 'border-slate-300 hover:bg-slate-50'
            }`}
          >
            <input
              type="radio"
              name="payment-method"
              value={opt.value}
              checked={method === opt.value}
              onChange={() => setMethod(opt.value)}
              className="accent-brand-600"
            />
            {opt.label}
          </label>
        ))}
      </div>

      <div className="mt-6 flex justify-end gap-2">
        <Button variant="secondary" size="sm" onClick={onClose} disabled={busy}>
          Cancelar
        </Button>
        <Button size="sm" onClick={() => onConfirm(method)} disabled={busy}>
          {busy ? 'Redirigiendo...' : 'Continuar al pago'}
        </Button>
      </div>
    </ModalShell>
  );
}
