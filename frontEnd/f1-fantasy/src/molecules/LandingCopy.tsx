import { F1Title, F1Text } from "../atoms";

export function LandingCopy() {
  return (
    <div className="w-full flex-1 text-center md:text-left">
      <F1Title level={1} className="!text-2xl sm:!text-3xl md:!text-4xl">F1 Fantasy</F1Title>
      <F1Title level={4} className="!mb-3 !mt-2 !font-normal sm:!mb-4">
        Private prediction leagues. Create or join a group, make your picks, and compete with friends.
      </F1Title>
      <F1Text className="block" muted>
        Sign in with Google, GitHub, Discord, or email to get started.
      </F1Text>
    </div>
  );
}
