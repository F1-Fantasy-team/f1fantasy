import { Button } from "antd";

type F1ButtonProps = React.ComponentProps<typeof Button>;

const primaryClass = "bg-f1-red hover:!bg-f1-red-dark border-0";
const textClass = "text-f1-silver hover:!text-f1-red";

export function F1Button({
  type = "default",
  className = "",
  ...props
}: F1ButtonProps) {
  const variantClass = type === "primary" ? primaryClass : type === "text" ? textClass : "";
  return (
    <Button
      type={type}
      className={`${variantClass} ${className}`.trim()}
      {...props}
    />
  );
}
