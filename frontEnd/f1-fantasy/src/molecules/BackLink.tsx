import { ArrowLeftOutlined } from "@ant-design/icons";
import { useSetRecoilState } from "recoil";
import { F1Button } from "../atoms";
import { selectedGroupIdState } from "../state/atoms";

export function BackLink() {
  const setSelectedGroupId = useSetRecoilState(selectedGroupIdState);
  return (
    <F1Button
      type="text"
      icon={<ArrowLeftOutlined />}
      onClick={() => setSelectedGroupId(null)}
      className="min-h-[44px] pl-0"
    >
      Back to groups
    </F1Button>
  );
}
