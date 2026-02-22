import type { ReactNode } from "react";
import { Navbar } from "../molecules";

type AuthTemplateProps = {
  children: ReactNode;
};

/** Full-width layout so the landing hero / banner can cover the entire viewport. */
export function AuthTemplate({ children }: AuthTemplateProps) {
  return (
    <div className="min-h-screen bg-f1-black text-f1-silver min-w-0">
      <Navbar />
      <main className="w-full">{children}</main>
    </div>
  );
}
