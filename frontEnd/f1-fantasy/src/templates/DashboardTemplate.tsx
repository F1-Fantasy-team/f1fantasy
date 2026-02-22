import type { ReactNode } from "react";
import { PageLayout } from "./PageLayout";

type DashboardTemplateProps = {
  children: ReactNode;
  /** Full-width block above main content (e.g. dashboard info banner). */
  topSection?: ReactNode;
};

export function DashboardTemplate({ children, topSection }: DashboardTemplateProps) {
  return <PageLayout topSection={topSection}>{children}</PageLayout>;
}
