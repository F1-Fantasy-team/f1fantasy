import { useState } from "react";
import { Select } from "antd";
import { F1Button, F1Text } from "../atoms";
import { useDrivers } from "../state/useDriversAndConstructors";
import type { ZeroPointersPrediction } from "../types/predictions";

type ZeroPointersEditorProps = {
  value: ZeroPointersPrediction | undefined;
  onSave: (prediction: ZeroPointersPrediction) => void;
};

export function ZeroPointersEditor({ value, onSave }: ZeroPointersEditorProps) {
  const drivers = useDrivers();
  const driverOptions = drivers.map((d) => ({ value: d.id, label: d.name }));
  const maxDriverCount = drivers.length;
  const [driverIds, setDriverIds] = useState<string[]>(value?.driverIds ?? []);

  const handleSave = () => {
    onSave({ driverIds });
  };

  return (
    <div className="space-y-4">
      <F1Text muted className="block text-xs">
        Pick 0–{maxDriverCount} drivers you think will score zero points. Use the dropdown to add or remove.
      </F1Text>
      <div className="min-w-48">
        <label className="mb-1 block text-xs text-f1-silver/70">Drivers (0–{maxDriverCount})</label>
        <Select
          placeholder="Select drivers"
          mode="multiple"
          value={driverIds}
          onChange={(ids) => setDriverIds(ids.slice(0, maxDriverCount))}
          options={driverOptions}
          className="w-full [&_.ant-select-selector]:bg-f1-gray [&_.ant-select-selector]:border-f1-gray [&_.ant-select-selection-item]:text-f1-silver f1-driver-select"
          classNames={{ popup: { root: "f1-driver-select-dropdown" } }}
          maxTagCount="responsive"
          showSearch
          filterOption={(input, option) =>
            (option?.label ?? "").toString().toLowerCase().includes(input.toLowerCase())
          }
          optionFilterProp="label"
        />
      </div>
      <F1Button type="primary" onClick={handleSave}>
        Save prediction
      </F1Button>
    </div>
  );
}
