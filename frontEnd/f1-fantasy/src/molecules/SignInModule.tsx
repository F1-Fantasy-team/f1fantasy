import { SignIn } from "@clerk/clerk-react";

export function SignInModule() {
  return (
    <div className="w-full min-w-0 max-w-sm flex-1 shrink self-center md:self-auto flex justify-center md:justify-end">
      <SignIn />
    </div>
  );
}
