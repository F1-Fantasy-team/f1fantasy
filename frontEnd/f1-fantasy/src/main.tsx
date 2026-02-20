import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { ClerkProvider } from "@clerk/clerk-react";
import "./index.css";
import App from "./App.tsx";
import { clerkF1Appearance } from "./theme/clerkAppearance";

const publishableKey = import.meta.env.VITE_CLERK_PUBLISHABLE_KEY;
if (!publishableKey) {
  console.warn(
    "Missing VITE_CLERK_PUBLISHABLE_KEY. Add it to .env to enable Clerk auth."
  );
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ClerkProvider publishableKey={publishableKey || ""} appearance={{ variables: clerkF1Appearance.variables }}>
      <App />
    </ClerkProvider>
  </StrictMode>
);
