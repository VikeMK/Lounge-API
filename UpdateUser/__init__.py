import logging
import json
import azure.functions as func

def main(
    req: func.HttpRequest,
    userToUpdate: func.DocumentList,
    existingUser: func.DocumentList,
    updatedUser: func.Out[func.Document]) -> func.HttpResponse:
    name = req.params.get('name')
    if not name:
        return func.HttpResponse("Please supply a name", status_code=400)

    new_name = req.params.get('new_name')
    if not new_name:
        return func.HttpResponse("Please supply a new name", status_code=400)

    if existingUser:
        return func.HttpResponse("A user with that name already exists", status_code=409)

    if not userToUpdate:
        return func.HttpResponse("No user exists with that name", status_code=404)

    # take the first user from the list since it will only contain one item
    userToUpdate = userToUpdate[0]

    # update their name
    userToUpdate["Name"] = new_name
    updatedUser.set(userToUpdate)

    resp_json = json.dumps({"Name": new_name, "ID": userToUpdate["ID"]})
    return func.HttpResponse(resp_json, status_code=200, mimetype="application/json")
