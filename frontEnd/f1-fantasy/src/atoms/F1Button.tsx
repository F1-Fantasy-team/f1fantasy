import { Button } from "antd";

type F1ButtonProps = React.ComponentProps<typeof Button>;

const baseClass = "font-medium";
const primaryClass =
  "bg-f1-red hover:!bg-f1-red-dark border-0 text-white disabled:!bg-f1-gray/70 disabled:!text-f1-silver/70 disabled:!border-f1-gray disabled:!cursor-not-allowed";
const textClass =
  "text-f1-silver hover:!text-f1-red disabled:!text-f1-silver/40";

export function F1Button({
  type = "default",
  className = "",
  ...props
}: F1ButtonProps) {
  const variantClass = type === "primary" ? primaryClass : type === "text" ? textClass : "";
  return (
    <Button
      type={type}
      className={`${baseClass} ${variantClass} ${className}`.trim()}
      {...props}
    />
  );
}
