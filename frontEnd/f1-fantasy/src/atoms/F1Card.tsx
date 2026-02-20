import { Card } from "antd";

type F1CardProps = React.ComponentProps<typeof Card> & {
  hoverable?: boolean;
};

const baseClass = "border-f1-gray bg-f1-carbon";

export function F1Card({
  className = "",
  hoverable,
  ...props
}: F1CardProps) {
  const hoverClass = hoverable ? "hover:!border-f1-red/50 transition-colors cursor-pointer" : "";
  return (
    <Card
      className={`${baseClass} ${hoverClass} ${className}`.trim()}
      hoverable={hoverable}
      {...props}
    />
  );
}
