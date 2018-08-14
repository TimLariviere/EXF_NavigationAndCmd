// Copyright 2018 Elmish.XamarinForms contributors. See LICENSE.md for license.
namespace EXF_NavigationAndCmd

open Elmish.XamarinForms
open Elmish.XamarinForms.DynamicViews
open Xamarin.Forms

module App = 
    open System
    
    type Msg = | GoToNextPage
               | GoBackAndUpdate
               | GoBackAndUpdateWorkaround
               | Update of DateTime
               | NavigationPopped

    type Model = 
        {
            PageModels: DateTime list
            Workaround: bool
            WorkaroundCmd: Cmd<Msg>
        }

    let init () =
        {
            PageModels = [ DateTime.Now ]
            Workaround = false
            WorkaroundCmd = Cmd.none
        }, Cmd.none

    let update msg model =
        match msg with
        | GoToNextPage ->
            { model with PageModels = DateTime.Now :: model.PageModels }, Cmd.none
        | GoBackAndUpdate ->
            { model with PageModels = model.PageModels.Tail }, Cmd.ofMsg (Update DateTime.Now)
        | GoBackAndUpdateWorkaround ->
            { model with PageModels = model.PageModels.Tail; Workaround = true; WorkaroundCmd = Cmd.ofMsg (Update DateTime.Now) }, Cmd.none
        | Update datetime ->
            { model with PageModels = datetime :: model.PageModels.Tail }, Cmd.none
        | NavigationPopped ->
            match model.Workaround with
            | true -> { model with Workaround = false; WorkaroundCmd = Cmd.none }, model.WorkaroundCmd
            | false -> model, Cmd.none

    let view (model: Model) dispatch =
        // Workaround iOS bug: https://github.com/xamarin/Xamarin.Forms/issues/3509
        let dispatchNavPopped =
            let mutable lastRemovedPageIdentifier: int = -1
            let apply dispatch (e: Xamarin.Forms.NavigationEventArgs) =
                let removedPageIdentifier = e.Page.GetHashCode()
                match lastRemovedPageIdentifier = removedPageIdentifier with
                | false ->
                    lastRemovedPageIdentifier <- removedPageIdentifier
                    dispatch NavigationPopped
                | true ->
                    ()
            apply

        let pages = 
            model.PageModels
            |> List.rev
            |> List.mapi (fun i date ->
                let str = date.ToString("yyyy/MM/dd hh:mm:ss")
                View.ContentPage(
                    title="Depth " + (string i),
                    content=View.StackLayout(
                        children=[
                            View.Label(text="Updated at " + str, verticalOptions=LayoutOptions.CenterAndExpand)
                            View.Button(text="Go to next page", command=(fun () -> dispatch GoToNextPage), verticalOptions=LayoutOptions.CenterAndExpand)
                            View.Button(text="Go back and update", command=(fun () -> dispatch GoBackAndUpdate), verticalOptions=LayoutOptions.CenterAndExpand)
                            View.Button(text="Go back and update workaround", command=(fun () -> dispatch GoBackAndUpdateWorkaround), verticalOptions=LayoutOptions.CenterAndExpand)
                        ]
                    )).HasBackButton(false))

        View.NavigationPage(
            popped=(dispatchNavPopped dispatch),
            pages=pages                
        )

type App () as app = 
    inherit Application ()

    let runner = 
        Program.mkProgram App.init App.update App.view
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> Program.runWithDynamicView app

