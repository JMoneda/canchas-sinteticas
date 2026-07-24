import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { buttonClasses } from './ui';

function navLinkClasses({ isActive }: { isActive: boolean }): string {
  return `rounded-[var(--radius-control)] px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-brand-50 text-brand-800' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
  }`;
}

export function Layout() {
  const { user, isAuthenticated, isOwner, logout } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/');
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-20 border-b border-slate-200/80 bg-white/80 backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
          <Link to="/" className="group flex items-center gap-2.5 font-display text-lg font-extrabold text-slate-900">
            <span className="grid h-9 w-9 place-items-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-soft transition-transform group-hover:scale-105">
              ⚽
            </span>
            <span>
              Canchas<span className="text-brand-600"> Sintéticas</span>
            </span>
          </Link>

          <nav className="hidden items-center gap-1 md:flex">
            <NavLink to="/" end className={navLinkClasses}>
              Buscar canchas
            </NavLink>
            {!isOwner && (
              <NavLink to="/partidos" className={navLinkClasses}>
                Partidos
              </NavLink>
            )}
            {isAuthenticated && !isOwner && (
              <NavLink to="/mis-reservas" className={navLinkClasses}>
                Mis reservas
              </NavLink>
            )}
            {isOwner && (
              <NavLink to="/panel" className={navLinkClasses}>
                Panel de dueño
              </NavLink>
            )}
          </nav>

          <div className="flex items-center gap-2">
            {isAuthenticated ? (
              <>
                <span className="hidden text-sm text-slate-500 sm:inline">
                  Hola, <span className="font-semibold text-slate-800">{user?.name}</span>
                </span>
                <button
                  onClick={handleLogout}
                  className="rounded-[var(--radius-control)] px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900"
                >
                  Salir
                </button>
              </>
            ) : (
              <>
                <Link
                  to="/login"
                  className="rounded-[var(--radius-control)] px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 hover:text-slate-900"
                >
                  Ingresar
                </Link>
                <Link to="/registro" className={buttonClasses('primary', 'sm')}>
                  Crear cuenta
                </Link>
              </>
            )}
          </div>
        </div>

        {/* Navegación en móvil */}
        <nav className="flex items-center gap-1 overflow-x-auto border-t border-slate-100 px-4 py-2 md:hidden">
          <NavLink to="/" end className={navLinkClasses}>
            Buscar
          </NavLink>
          {!isOwner && (
            <NavLink to="/partidos" className={navLinkClasses}>
              Partidos
            </NavLink>
          )}
          {isAuthenticated && !isOwner && (
            <NavLink to="/mis-reservas" className={navLinkClasses}>
              Mis reservas
            </NavLink>
          )}
          {isOwner && (
            <NavLink to="/panel" className={navLinkClasses}>
              Panel
            </NavLink>
          )}
        </nav>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8">
        <Outlet />
      </main>

      <footer className="border-t border-slate-200/80 bg-white/60 py-6 text-center text-xs text-slate-400">
        Canchas Sintéticas · Plataforma de reservas · MVP demostrativo
      </footer>
    </div>
  );
}
