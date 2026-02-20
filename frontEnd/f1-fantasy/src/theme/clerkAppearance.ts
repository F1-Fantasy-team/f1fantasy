/**
 * Clerk appearance variables for F1 dark theme.
 * High-contrast so all text, icons (e.g. GitHub), and footer are readable.
 */
export const clerkF1Appearance = {
  elements: {
    rootBox: "w-full clerk-f1-theme",
    card: "w-full shadow-none bg-transparent",
  },
  variables: {
    colorPrimary: "#e10600",
    colorPrimaryForeground: "#ffffff",
    colorForeground: "#e5e5e5",
    /* Secondary text: "Don't have an account?", "Secured by", etc. */
    colorMutedForeground: "#c4c4c4",
    colorBackground: "#1a1a1a",
    colorInput: "#2d2d2d",
    colorInputForeground: "#e5e5e5",
    colorBorder: "#2d2d2d",
    colorMuted: "#383838",
    colorNeutral: "#454545",
    colorRing: "#e10600",
    borderRadius: "6px",
  },
} as const;
