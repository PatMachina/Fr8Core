# ACTIVITIES – SIGNALLING STATUS
[Go to Contents](https://github.com/Fr8org/Fr8Core/blob/master/Docs/Home.md) 

Activities signal information in several ways:

1.  They signal the UI they want shown to users by configuring a Crate of UI Controls and addit it to the Activity JSON.
2.  At Run-time, they signal their preferred flow control by providing [Activity Responses](https://github.com/Fr8org/Fr8Core/blob/master/Docs/ForDevelopers/Objects/Activities/ActivityResponses.md) in their HTTP responses. These can have dramatic effects like shutting down the entire Plan.
3.  At Design-time, they signal the Crates and Fields that they intend to make available at Run-Time, so that downstream Activities can respond (for example, by enabling mapping from one field to another). 



[Go to Contents](https://github.com/Fr8org/Fr8Core/blob/master/Docs/Home.md) 