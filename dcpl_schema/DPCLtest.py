import json
import jsonschema

with open('DPCLschema.json') as schemaFile:
    schema = json.load(schemaFile)
    v = jsonschema.Draft202012Validator.check_schema(schema)
    print("Schema is valid")

# with open('DPCLexamples.json') as dataFile:
#     data = json.load(dataFile)
#     jsonschema.Draft202012Validator(schema).validate(data)
#     print("Validation of test set passed.")

with open('example.json') as dataFile:
    data = json.load(dataFile)
    jsonschema.Draft202012Validator(schema).validate(data)
    print("Validation passed.")