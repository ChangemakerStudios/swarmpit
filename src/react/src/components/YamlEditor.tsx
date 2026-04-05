import CodeMirror from "@uiw/react-codemirror";
import { yaml } from "@codemirror/lang-yaml";
import { linter, type Diagnostic } from "@codemirror/lint";
import { parse } from "yaml";
import { useTheme } from "@mui/material";

const yamlLinter = linter((view) => {
  const diagnostics: Diagnostic[] = [];
  const doc = view.state.doc.toString();
  if (!doc.trim()) return diagnostics;

  try {
    parse(doc, { strict: true });
  } catch (e: any) {
    const pos = e.linePos?.[0];
    const from = pos ? view.state.doc.line(pos.line).from + (pos.col - 1) : 0;
    diagnostics.push({
      from,
      to: from + 1,
      severity: "error",
      message: e.message?.split("\n")[0] ?? "Invalid YAML",
    });
  }

  return diagnostics;
});

interface YamlEditorProps {
  value: string;
  onChange: (value: string) => void;
  minHeight?: string;
  readOnly?: boolean;
  placeholder?: string;
}

export default function YamlEditor({
  value,
  onChange,
  minHeight = "400px",
  readOnly = false,
  placeholder,
}: YamlEditorProps) {
  const theme = useTheme();
  const isDark = theme.palette.mode === "dark";

  return (
    <CodeMirror
      value={value}
      onChange={onChange}
      extensions={[yaml(), yamlLinter]}
      theme={isDark ? "dark" : "light"}
      readOnly={readOnly}
      placeholder={placeholder}
      minHeight={minHeight}
      basicSetup={{
        lineNumbers: true,
        foldGutter: true,
        bracketMatching: true,
        indentOnInput: true,
      }}
      style={{
        fontSize: "0.85rem",
        border: `1px solid ${theme.palette.divider}`,
        borderRadius: 4,
      }}
    />
  );
}
