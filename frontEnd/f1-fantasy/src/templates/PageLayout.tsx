import type { ReactNode } from "react";
import { Navbar } from "../molecules";

type PageLayoutProps = {
  children: ReactNode;
};

export function PageLayout({ children }: PageLayoutProps) {
  return (
    <div className="min-h-screen bg-f1-black text-f1-silver min-w-0">
      <Navbar />
      <main className="container mx-auto w-full max-w-4xl px-3 py-6 sm:px-4 sm:py-8">{children}</main>
    </div>
  );
}
