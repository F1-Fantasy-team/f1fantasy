import { useState } from "react";
import { Input } from "antd";
import { F1Button, F1Text } from "../atoms";
import type { WildcardPrediction } from "../types/predictions";

const { TextArea } = Input;

type WildcardEditorProps = {
  value: WildcardPrediction | undefined;
  onSave: (prediction: WildcardPrediction) => void;
};

/** One wildcard prediction per user: single statement. */
export function WildcardEditor({ value, onSave }: WildcardEditorProps) {
  const [statement, setStatement] = useState(value?.statement ?? "");
  const trimmed = statement.trim();

  const handleSave = () => {
    if (!trimmed) return;
    onSave({ ...value, statement: trimmed });
  };

  return (
    <div className="space-y-4">
      <F1Text muted className="block text-xs">
        One wildcard prediction per user. The bolder the claim, the more points the admin can assign if it comes true.
      </F1Text>
      <TextArea
        placeholder="e.g. Piastri wins a race, or Haas scores a podium..."
        value={statement}
        onChange={(e) => setStatement(e.target.value)}
        rows={3}
        maxLength={500}
        showCount
        className="bg-f1-gray/80 border-f1-gray text-f1-silver placeholder:text-f1-silver/50"
      />
      <F1Button type="primary" onClick={handleSave} disabled={!trimmed}>
        Save wildcard
      </F1Button>
    </div>
  );
}
