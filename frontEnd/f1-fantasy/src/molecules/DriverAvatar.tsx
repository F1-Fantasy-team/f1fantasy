import { useDrivers } from "../state/useDriversAndConstructors";

/** Stable background color from driver id (no external image needed). */
const AVATAR_COLORS = [
  "#1e3a5f", "#374151", "#4b5563", "#6b7280",
  "#7c2d12", "#14532d", "#134e4a", "#1e40af",
  "#5b21b6", "#831843", "#9f1239", "#b45309",
];

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  return (name.slice(0, 2) || "?").toUpperCase();
}

function avatarColor(driverId: string): string {
  let n = 0;
  for (let i = 0; i < driverId.length; i++) n += driverId.charCodeAt(i);
  return AVATAR_COLORS[n % AVATAR_COLORS.length];
}

type DriverAvatarProps = {
  driverId: string;
  size?: number;
  showName?: boolean;
  className?: string;
};

export function DriverAvatar({ driverId, size = 48, showName = true, className = "" }: DriverAvatarProps) {
  const drivers = useDrivers();
  const driver = drivers.find((d) => d.id === driverId);
  const name = driver?.name ?? driverId;
  const initials = getInitials(name);
  const bg = avatarColor(driverId);

  return (
    <div className={`flex min-w-0 items-center gap-2 ${className}`}>
      <div
        role="img"
        aria-label={name}
        className="shrink-0 rounded-full border border-f1-gray flex items-center justify-center font-semibold text-white select-none"
        style={{
          width: size,
          height: size,
          backgroundColor: bg,
          fontSize: Math.max(10, Math.floor(size * 0.4)),
        }}
      >
        {initials}
      </div>
      {showName && <span className="min-w-0 truncate text-sm text-f1-silver">{name}</span>}
    </div>
  );
}
