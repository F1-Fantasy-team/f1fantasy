import { Typography } from "antd";

const { Title } = Typography;

type F1TitleProps = React.ComponentProps<typeof Title> & {
  level?: 1 | 2 | 3 | 4 | 5;
};

const levelClasses: Record<number, string> = {
  1: "!text-f1-white !mb-4",
  2: "!text-f1-white !mb-0",
  3: "!text-f1-white !mb-2",
  4: "!text-f1-silver",
  5: "!text-f1-white !mb-1",
};

export function F1Title({ level = 1, className = "", ...props }: F1TitleProps) {
  return (
    <Title
      level={level}
      className={`${levelClasses[level]} ${className}`.trim()}
      {...props}
    />
  );
}
