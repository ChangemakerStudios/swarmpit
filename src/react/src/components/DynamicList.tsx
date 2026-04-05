import { type ReactNode } from "react";
import { Box, Button, IconButton, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";

interface DynamicListProps<T> {
  items: T[];
  onChange: (items: T[]) => void;
  renderItem: (
    item: T,
    index: number,
    onChange: (field: string, value: any) => void
  ) => ReactNode;
  defaultItem: T;
  title?: string;
  emptyMessage?: string;
  addLabel?: string;
}

export default function DynamicList<T>({
  items,
  onChange,
  renderItem,
  defaultItem,
  title,
  emptyMessage = "No items",
  addLabel = "Add",
}: DynamicListProps<T>) {
  const handleAdd = () => {
    onChange([...items, { ...defaultItem }]);
  };

  const handleRemove = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
  };

  const handleFieldChange = (index: number, field: string, value: any) => {
    const updated = items.map((item, i) =>
      i === index ? { ...item, [field]: value } : item
    );
    onChange(updated);
  };

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 1 }}>
        {title && (
          <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
            {title}
          </Typography>
        )}
        <Button startIcon={<AddIcon />} onClick={handleAdd} size="small">
          {addLabel}
        </Button>
      </Box>
      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ py: 1, textAlign: "center" }}>
          {emptyMessage}
        </Typography>
      ) : (
        items.map((item, index) => (
          <Box
            key={index}
            sx={{ display: "flex", alignItems: "flex-start", gap: 1, mb: 1 }}
          >
            <Box sx={{ flexGrow: 1 }}>
              {renderItem(item, index, (field, value) =>
                handleFieldChange(index, field, value)
              )}
            </Box>
            <IconButton size="small" onClick={() => handleRemove(index)} sx={{ mt: 0.5 }}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Box>
        ))
      )}
    </Box>
  );
}
