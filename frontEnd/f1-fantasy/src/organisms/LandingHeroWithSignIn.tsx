import { LandingCopy, SignInModule } from "../molecules";

/** Inline hero graphic so it works without external image requests (no CORS/blocking). */
function HeroGraphic() {
  return (
    <div
      className="h-40 w-full max-w-md rounded-lg border border-f1-gray shadow-lg overflow-hidden sm:h-48 md:h-56"
      aria-hidden
    >
      <svg
        viewBox="0 0 400 240"
        className="h-full w-full object-cover"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <defs>
          <linearGradient id="heroBg" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#0f172a" />
            <stop offset="50%" stopColor="#1e293b" />
            <stop offset="100%" stopColor="#0f172a" />
          </linearGradient>
          <linearGradient id="heroAccent" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="#dc2626" stopOpacity="0.9" />
            <stop offset="100%" stopColor="#b91c1c" stopOpacity="0.6" />
          </linearGradient>
        </defs>
        <rect width="400" height="240" fill="url(#heroBg)" />
        <ellipse cx="200" cy="140" rx="160" ry="50" fill="url(#heroAccent)" opacity="0.25" />
        <path
          d="M40 180 Q120 100 200 120 T360 100"
          stroke="#dc2626"
          strokeWidth="3"
          strokeOpacity="0.6"
          fill="none"
        />
        <circle cx="80" cy="175" r="8" fill="#dc2626" opacity="0.9" />
        <circle cx="200" cy="115" r="8" fill="#dc2626" opacity="0.9" />
        <circle cx="320" cy="95" r="8" fill="#dc2626" opacity="0.9" />
      </svg>
    </div>
  );
}

export function LandingHeroWithSignIn() {
  return (
    <div className="flex min-h-[70vh] flex-col items-center justify-center gap-6 py-6 sm:min-h-[80vh] sm:gap-10 md:flex-row md:gap-12">
      <LandingCopy />
      <div className="flex w-full max-w-full flex-col items-center gap-4 sm:gap-6 md:w-auto">
        <HeroGraphic />
        <SignInModule />
      </div>
    </div>
  );
}
