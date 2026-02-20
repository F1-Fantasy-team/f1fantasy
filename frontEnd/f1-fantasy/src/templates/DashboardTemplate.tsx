import type { ReactNode } from "react";
import { PageLayout } from "./PageLayout";

type DashboardTemplateProps = {
  children: ReactNode;
};

export function DashboardTemplate({ children }: DashboardTemplateProps) {
  return <PageLayout>{children}</PageLayout>;
}
