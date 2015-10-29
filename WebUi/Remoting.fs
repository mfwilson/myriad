namespace WebUi

open WebSharper

module Server =

    [<Rpc>]
    let DoSomething input =
        let R (s: string) = System.String(Array.rev(s.ToCharArray()))
        async {
            return R input
        }

    [<Rpc>]
    let GetDimensions() =
        [ 
            "Environment", [ "PROD"; "UAT"; "DEV" ];
            "Location", [ "Chicago"; "New York"; "London"; "Amsterdam" ];
            "Application", [ "Rook"; "Knight"; "Pawn"; "Bishop" ];
            "Instance", [ "mary"; "jimmy"; "rex"; "paulie" ];
        ]