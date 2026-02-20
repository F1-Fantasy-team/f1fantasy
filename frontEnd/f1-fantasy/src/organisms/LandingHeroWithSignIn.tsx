import { LandingCopy, SignInModule } from "../molecules";

/** Real F1 car photo (Wikimedia Commons, CC). Williams FW38 – replace with any F1 image URL if needed. */
const HERO_IMAGE_URL =
  "https://upload.wikimedia.org/wikipedia/commons/c/c5/2016_Williams_FW38_Formula_1_Car_%2853436323345%29.jpg";

/** Hero background: actual F1 image with dark overlay for text readability. */
function HeroBannerBackground() {
  return (
    <div className="absolute inset-0 overflow-hidden bg-f1-black" aria-hidden>
      <img
        src={HERO_IMAGE_URL}
        alt=""
        className="absolute inset-0 h-full w-full object-cover"
        loading="eager"
        fetchPriority="high"
      />
      {/* Dark overlay so text and sign-in card stay readable */}
      <div className="absolute inset-0 bg-black/55" />
    </div>
  );
}

export function LandingHeroWithSignIn() {
  return (
    <section className="relative flex min-h-[70vh] flex-col items-center justify-center py-10 sm:min-h-[80vh] sm:py-14">
      <HeroBannerBackground />
      <div className="relative z-10 flex w-full max-w-5xl flex-col items-stretch gap-8 px-4 sm:gap-10 md:flex-row md:items-center md:justify-between md:gap-12 lg:px-8">
        <div className="w-full min-w-0 md:max-w-md">
          <LandingCopy />
        </div>
        <div className="w-full min-w-0 flex justify-center md:justify-end">
          <SignInModule />
        </div>
      </div>
    </section>
  );
}
