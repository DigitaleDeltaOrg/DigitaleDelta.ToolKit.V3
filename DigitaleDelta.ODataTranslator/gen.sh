#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
java -jar antlr-4.13.2-complete.jar -Dlanguage=CSharp -Xexact-output-dir grammar/OData.g4 -o generated
