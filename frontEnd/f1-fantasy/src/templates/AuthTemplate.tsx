import type { ReactNode } from "react";
import { PageLayout } from "./PageLayout";

type AuthTemplateProps = {
  children: ReactNode;
};

export function AuthTemplate({ children }: AuthTemplateProps) {
  return <PageLayout>{children}</PageLayout>;
}
