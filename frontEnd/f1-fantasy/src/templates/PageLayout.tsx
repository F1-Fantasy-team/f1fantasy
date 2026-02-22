import type { ReactNode } from "react";
import { Navbar } from "../molecules";

type PageLayoutProps = {
  children: ReactNode;
  /** Rendered full-width above main (e.g. dashboard info banner). */
  topSection?: ReactNode;
};

export function PageLayout({ children, topSection }: PageLayoutProps) {
  return (
    <div className="min-h-screen bg-f1-black text-f1-silver min-w-0">
      <Navbar />
      {topSection != null ? <div className="w-full">{topSection}</div> : null}
      <main className="container mx-auto w-full max-w-4xl px-3 py-6 sm:px-4 sm:py-8">{children}</main>
    </div>
  );
}
