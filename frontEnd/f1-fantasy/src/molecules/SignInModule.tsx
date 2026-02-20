import { useEffect } from "react";
import { SignIn } from "@clerk/clerk-react";
import { F1Card } from "../atoms";
import { clerkF1Appearance } from "../theme/clerkAppearance";

function applyGitHubButtonStyles() {
  const btn = document.querySelector(
    "button.cl-socialButtonsIconButton__github, button.cl-button__github"
  ) as HTMLElement | null;
  if (!btn) return false;
  btn.style.setProperty("color", "#ffffff", "important");
  btn.style.setProperty("opacity", "1", "important");
  const svg = btn.querySelector("svg");
  if (svg) {
    svg.style.setProperty("filter", "brightness(0) invert(1)", "important");
    svg.style.setProperty("fill", "#ffffff", "important");
    svg.querySelectorAll("path, g").forEach((el) => {
      (el as HTMLElement).style.setProperty("fill", "#ffffff", "important");
    });
  }
  return true;
}

export function SignInModule() {
  useEffect(() => {
    let cancelled = false;
    const tryApply = () => {
      if (cancelled) return;
      if (applyGitHubButtonStyles()) return;
      requestAnimationFrame(tryApply);
    };
    const t = setTimeout(tryApply, 100);
    const observer = new MutationObserver(() => {
      if (!cancelled) applyGitHubButtonStyles();
    });
    observer.observe(document.body, { childList: true, subtree: true });
    return () => {
      cancelled = true;
      clearTimeout(t);
      observer.disconnect();
    };
  }, []);

  return (
    <F1Card
      className="sign-in-card w-full min-w-0 max-w-md flex-1 shrink self-center md:self-auto"
      styles={{ body: { padding: "1rem", overflow: "visible", background: "#1a1a1a" } }}
    >
      <div className="min-w-0 w-full sign-in-fill-card">
        <SignIn appearance={clerkF1Appearance} />
      </div>
    </F1Card>
  );
}
