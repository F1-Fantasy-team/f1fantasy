import { TeamOutlined } from "@ant-design/icons";
import type { ReactNode } from "react";
import { F1Card, F1Title, F1Text, F1Button } from "../atoms";

type EmptyStateProps = {
  title: string;
  description: string;
  buttonLabel: string;
  buttonIcon?: ReactNode;
  onAction: () => void;
};

export function EmptyState({
  title,
  description,
  buttonLabel,
  buttonIcon,
  onAction,
}: EmptyStateProps) {
  return (
    <F1Card className="min-w-0 py-8 text-center sm:py-12">
      <TeamOutlined className="text-5xl text-f1-gray mb-4" />
      <F1Title level={4}>{title}</F1Title>
      <F1Text muted className="block mb-6">
        {description}
      </F1Text>
      <F1Button type="primary" icon={buttonIcon} onClick={onAction}>
        {buttonLabel}
      </F1Button>
    </F1Card>
  );
}
