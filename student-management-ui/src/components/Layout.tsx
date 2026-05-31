import { NavLink, Outlet } from 'react-router-dom';

const NAV = [
  { to: '/', label: 'Sohbet', icon: '💬', end: true },
  { to: '/students', label: 'Öğrenciler', icon: '🎓', end: false },
  { to: '/documents', label: 'Belgeler', icon: '📄', end: false },
];

export function Layout() {
  return (
    <>
      <div className="app-bg" />
      <div className="flex h-screen overflow-hidden">
        {/* Sidebar */}
        <aside className="flex w-60 shrink-0 flex-col gap-1 px-3 py-6 relative"
          style={{
            background: 'linear-gradient(180deg, rgba(15,12,34,0.98) 0%, rgba(20,14,50,0.98) 100%)',
            borderRight: '1px solid rgba(99,102,241,0.15)',
          }}
        >
          {/* Logo */}
          <div className="mb-8 px-2">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-xl"
                style={{ background: 'linear-gradient(135deg, #6366f1, #8b5cf6)' }}>
                <span className="text-base">🎓</span>
              </div>
              <div>
                <p className="text-xs font-bold text-white tracking-wide">SMS</p>
                <p className="text-[10px] text-indigo-300/60 tracking-widest uppercase">Yönetim</p>
              </div>
            </div>
          </div>

          <p className="mb-2 px-2 text-[10px] font-semibold uppercase tracking-widest text-indigo-300/40">
            Navigasyon
          </p>

          {NAV.map(({ to, label, icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition-all duration-200 ${
                  isActive
                    ? 'text-white shadow-lg shadow-indigo-500/20'
                    : 'text-indigo-200/50 hover:text-indigo-100/80'
                }`
              }
              style={({ isActive }) => isActive ? {
                background: 'linear-gradient(135deg, rgba(99,102,241,0.25), rgba(139,92,246,0.15))',
                borderLeft: '2px solid #818cf8',
              } : {
                borderLeft: '2px solid transparent',
              }}
            >
              <span className="text-base">{icon}</span>
              {label}
            </NavLink>
          ))}

          {/* Bottom decoration */}
          <div className="mt-auto">
            <div className="rounded-xl p-3"
              style={{
                background: 'rgba(99,102,241,0.08)',
                border: '1px solid rgba(99,102,241,0.15)',
              }}
            >
              <p className="text-[10px] text-indigo-300/50 text-center">Öğrenci Yönetim Sistemi</p>
            </div>
          </div>
        </aside>

        {/* Page content */}
        <main className="flex flex-1 flex-col overflow-hidden">
          <Outlet />
        </main>
      </div>
    </>
  );
}
