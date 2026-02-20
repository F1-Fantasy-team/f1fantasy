import { Typography } from "antd";

const { Text } = Typography;

type F1TextProps = React.ComponentProps<typeof Text> & {
  muted?: boolean;
};

export function F1Text({ muted, className = "", ...props }: F1TextProps) {
  const colorClass = muted ? "text-f1-silver/70" : "text-f1-silver";
  return (
    <Text className={`${colorClass} ${className}`.trim()} {...props} />
  );
}
