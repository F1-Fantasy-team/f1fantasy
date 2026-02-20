import { TeamOutlined } from "@ant-design/icons";
import type { ReactNode } from "react";
import { F1Card, F1Title, F1Text, F1Button } from "../atoms";

type EmptyStateProps = {
  title: string;
  description: string;
  buttonLabel: string;
  buttonIcon?: ReactNode;
  onAction: () => void;
  /** Optional illustration (e.g. F1-themed SVG) shown above the icon */
  illustration?: ReactNode;
};

export function EmptyState({
  title,
  description,
  buttonLabel,
  buttonIcon,
  onAction,
  illustration,
}: EmptyStateProps) {
  return (
    <F1Card className="min-w-0 py-8 text-center sm:py-12">
      {illustration && <div className="mb-4 flex justify-center">{illustration}</div>}
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
