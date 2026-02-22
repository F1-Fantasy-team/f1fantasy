import type { ReactNode } from "react";
import { useRecoilValue } from "recoil";
import { Navbar } from "../molecules";
import { appDataLoadingState } from "../state/atoms";

type PageLayoutProps = {
  children: ReactNode;
  /** Rendered full-width above main (e.g. dashboard info banner). */
  topSection?: ReactNode;
};

export function PageLayout({ children, topSection }: PageLayoutProps) {
  const appDataLoading = useRecoilValue(appDataLoadingState);

  return (
    <div className="min-h-screen bg-f1-black text-f1-silver min-w-0">
      <Navbar />
      {appDataLoading && (
        <div className="sticky top-0 z-10 h-1 w-full overflow-hidden bg-f1-carbon" role="progressbar" aria-valuenow={undefined} aria-label="Loading">
          <div className="h-full w-1/3 bg-f1-red opacity-90" style={{ animation: "loading-bar 1.5s ease-in-out infinite" }} />
        </div>
      )}
      {topSection != null ? <div className="w-full">{topSection}</div> : null}
      <main className="container mx-auto w-full max-w-4xl px-3 py-6 sm:px-4 sm:py-8">{children}</main>
    </div>
  );
}
