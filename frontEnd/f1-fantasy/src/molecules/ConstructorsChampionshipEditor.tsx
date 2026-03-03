import { useState } from "react";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { HolderOutlined } from "@ant-design/icons";
import { F1Button, F1Text } from "../atoms";
import { useConstructors, useConstructorsFromApi } from "../state/useDriversAndConstructors";
import type { ConstructorsChampionshipPrediction } from "../types/predictions";

function SortableConstructorRow({
  constructorId,
  position,
  constructorName,
}: {
  constructorId: string;
  position: number;
  constructorName: string;
}) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: constructorId });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <tr
      ref={setNodeRef}
      style={style}
      className={`border-b border-f1-gray/50 ${isDragging ? "opacity-50 bg-f1-gray/50 z-10" : ""}`}
    >
      <td className="py-1.5 pr-3 tabular-nums text-f1-silver/80 w-10">{position}</td>
      <td className="py-1.5 pr-2">
        <span
          className="inline-flex cursor-grab touch-none items-center gap-2 active:cursor-grabbing"
          {...attributes}
          {...listeners}
        >
          <HolderOutlined className="text-f1-silver/60" />
          <span className="text-f1-silver">{constructorName}</span>
        </span>
      </td>
    </tr>
  );
}

type ConstructorsChampionshipEditorProps = {
  value: ConstructorsChampionshipPrediction | undefined;
  onSave: (prediction: ConstructorsChampionshipPrediction) => void;
};

export function ConstructorsChampionshipEditor({ value, onSave }: ConstructorsChampionshipEditorProps) {
  const constructors = useConstructors();
  const fromApi = useConstructorsFromApi();
  const sorted = (value ?? []).slice().sort((a, b) => a.position - b.position);
  const existingIds = sorted.map((e) => e.constructorId);
  const missingIds = constructors.map((c) => c.id).filter((id) => !existingIds.includes(id));
  const count = constructors.length;
  const [constructorOrder, setConstructorOrder] = useState<string[]>(() => {
    if (existingIds.length >= count) return existingIds.slice(0, count);
    return [...existingIds, ...missingIds].slice(0, count);
  });
  const getConstructorName = (id: string) => constructors.find((c) => c.id === id)?.name ?? id;

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over == null || active.id === over.id) return;
    setConstructorOrder((prev) => {
      const oldIndex = prev.indexOf(active.id as string);
      const newIndex = prev.indexOf(over.id as string);
      if (oldIndex === -1 || newIndex === -1) return prev;
      return arrayMove(prev, oldIndex, newIndex);
    });
  };

  const handleSave = () => {
    const prediction: ConstructorsChampionshipPrediction = constructorOrder.map((constructorId, i) => ({
      position: i + 1,
      constructorId,
    }));
    onSave(prediction);
  };

  const isValid =
    constructorOrder.length === count &&
    new Set(constructorOrder).size === count &&
    constructorOrder.every((id) => constructors.some((c) => c.id === id));

  if (constructors.length === 0) {
    return (
      <div className="space-y-2">
        <F1Text muted className="block text-sm">
          No constructors are currently available. Please try again later or contact the administrator.
        </F1Text>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <F1Text muted className="block text-xs">
        {`Drag constructors to reorder. Top = P1, bottom = P${count}.`}
        {fromApi
          ? ` (${constructors.length} constructors loaded)`
          : ` (${constructors.length} constructors available)`}
      </F1Text>
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <div className="min-w-0 overflow-x-auto">
          <table className="w-full min-w-[200px] text-sm">
            <thead>
              <tr className="border-b border-f1-gray text-left text-f1-silver/70">
                <th className="py-1 pr-3 font-normal w-10">Pos</th>
                <th className="py-1 font-normal">Constructor</th>
              </tr>
            </thead>
            <tbody>
              <SortableContext items={constructorOrder} strategy={verticalListSortingStrategy}>
                {constructorOrder.map((constructorId, i) => (
                  <SortableConstructorRow
                    key={constructorId}
                    constructorId={constructorId}
                    position={i + 1}
                    constructorName={getConstructorName(constructorId)}
                  />
                ))}
              </SortableContext>
            </tbody>
          </table>
        </div>
      </DndContext>
      <F1Button type="primary" onClick={handleSave} disabled={!isValid}>
        Save prediction
      </F1Button>
      {!isValid && constructorOrder.length < count && (
        <F1Text muted className="block text-xs">
          Add all {count} constructors (drag to reorder; list must be complete to save).
        </F1Text>
      )}
    </div>
  );
}
