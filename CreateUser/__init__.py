import logging
import uuid
import json
import azure.functions as func

def main(req: func.HttpRequest, existingUser: func.DocumentList, newUser: func.Out[func.Document]) -> func.HttpResponse:
    name = req.params.get('name')
    if not name:
        return func.HttpResponse("Please supply a name", status_code=400)

    mkc_id = req.params.get('mkc_id')
    if not mkc_id:
        return func.HttpResponse("Please supply a Mario Kart Central ID", status_code=400)

    if existingUser:
        return func.HttpResponse("A user with that name already exists", status_code=409)

    # create a unique ID for the user to be used internally
    user_id = str(uuid.uuid4())
    row = {
        "PartitionKey": "users",
        "ID": user_id,
        "Name": name,
        "MKC": mkc_id
    }

    newUser.set(func.Document.from_dict(row))
    resp_json = json.dumps({"Name": name, "ID": user_id})
    return func.HttpResponse(resp_json, status_code=200, mimetype="application/json")
