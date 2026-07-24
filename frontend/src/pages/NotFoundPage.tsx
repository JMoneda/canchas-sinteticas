import { Link } from 'react-router-dom';
import { buttonClasses } from '../components/ui';

export function NotFoundPage() {
  return (
    <div className="py-20 text-center">
      <p className="text-6xl">🔍</p>
      <h1 className="mt-4 text-2xl font-bold text-slate-800">Página no encontrada</h1>
      <p className="mt-1 text-slate-500">La ruta que buscas no existe.</p>
      <Link to="/" className={buttonClasses('primary', 'md', 'mt-6')}>
        Volver al inicio
      </Link>
    </div>
  );
}
