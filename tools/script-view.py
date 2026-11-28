#!/usr/bin/env python3
"""Print a Blueprint function of the installed game as readable lines.

Usage: tools/script-view.py <package> [<function>] [--grep REGEX]

Runs `bloodline script` and renders each statement as `index: text`, with assignments, calls, jumps and spawns
written out, so a reading can be checked without decoding the JSON by hand.
"""

import json
import re
import subprocess
import sys


def short(path):
    if not isinstance(path, str):
        return str(path)
    return path.split(":")[-1].split(".")[-1].split("/")[-1]


def field(node):
    variable = node.get("Variable") or node.get("Property") or {}
    name = short(variable.get("$field", "?"))
    return name


def render(node):
    if node is None:
        return "None"
    if not isinstance(node, dict):
        return str(node)
    token = node.get("Token", "")
    if token in ("EX_LocalVariable", "EX_InstanceVariable", "EX_DefaultVariable", "EX_LocalOutVariable"):
        return field(node)
    if token in ("EX_IntConst", "EX_FloatConst", "EX_ByteConst", "EX_Int64Const"):
        return str(node.get("Value"))
    if token in ("EX_StringConst", "EX_UnicodeStringConst"):
        return json.dumps(node.get("Value"))
    if token == "EX_NameConst":
        return "'" + str(node.get("Value")) + "'"
    if token == "EX_TextConst":
        text = re.search(r'"(?:SourceString|Value)":\s*"([^"]*)"', json.dumps(node.get("Value")))
        return 'T"' + (text.group(1) if text else "") + '"'
    if token == "EX_ObjectConst":
        value = node.get("Value")
        return "<" + short(value.get("$object") if isinstance(value, dict) else value) + ">"
    if token == "EX_True":
        return "true"
    if token == "EX_False":
        return "false"
    if token in ("EX_Self",):
        return "self"
    if token in ("EX_NoObject", "EX_Nothing"):
        return "null"
    if token in ("EX_Let", "EX_LetBool", "EX_LetObj", "EX_LetWeakObjPtr", "EX_LetValueOnPersistentFrame"):
        target = node.get("Variable") or node.get("Property")
        left = render(target) if isinstance(target, dict) and "Token" in target else short((target or {}).get("$field"))
        return left + " = " + render(node.get("Assignment") or node.get("Expression"))
    if token == "EX_Context" or token == "EX_ClassContext":
        return render(node.get("ObjectExpression")) + "." + render(node.get("ContextExpression"))
    if token == "EX_StructMemberContext":
        return render(node.get("StructExpression")) + "." + short((node.get("StructMemberExpression") or {}).get("$field"))
    if token in ("EX_CallMath", "EX_FinalFunction", "EX_LocalFinalFunction"):
        name = short((node.get("StackNode") or {}).get("$object"))
        return name + "(" + ", ".join(render(p) for p in node.get("Parameters", [])) + ")"
    if token in ("EX_VirtualFunction", "EX_LocalVirtualFunction"):
        return node.get("VirtualFunctionName", "?") + "(" + ", ".join(render(p) for p in node.get("Parameters", [])) + ")"
    if token == "EX_JumpIfNot":
        return "if not (" + render(node.get("BooleanExpression")) + ") goto " + str(node.get("CodeOffset"))
    if token == "EX_Jump":
        return "goto " + str(node.get("CodeOffset"))
    if token == "EX_PopExecutionFlowIfNot":
        return "if not (" + render(node.get("BooleanExpression")) + ") pop"
    if token == "EX_PushExecutionFlow":
        return "push " + str(node.get("PushingAddress"))
    if token == "EX_PopExecutionFlow":
        return "pop"
    if token in ("EX_DynamicCast", "EX_MetaCast"):
        return "cast<" + short((node.get("ClassPtr") or {}).get("$object")) + ">(" + render(node.get("Target")) + ")"
    if token == "EX_SetArray":
        return render(node.get("AssigningProperty")) + " = [" + ", ".join(render(p) for p in node.get("Elements", [])) + "]"
    if token == "EX_Return":
        return "return"
    if token == "EX_EndOfScript":
        return "end"
    return token


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    grep = None
    if "--grep" in sys.argv:
        grep = re.compile(sys.argv[sys.argv.index("--grep") + 1])
        args = [a for a in args if a != grep.pattern]
    command = ["dotnet", "run", "--no-build", "--project", "src/Bloodline", "--", "script", *args]
    output = subprocess.run(command, capture_output=True, text=True, check=True).stdout
    if len(args) == 1:
        print(output)
        return
    for statement in json.loads(output):
        line = render(statement)
        line = re.sub(r"\w+ = BeginDeferredActorSpawnFromClass\(self, ([^,]+),.*", r"SPAWN \1", line)
        if line in ("pop", "end") or line.startswith("push "):
            continue
        if grep and not grep.search(line):
            continue
        print(f"{statement.get('StatementIndex')}: {line[:400]}")


main()
