package com.homeaffairs;

public class HomeAffairsApplication {

    public static void main(String[] args) throws Exception {
        int port = args.length > 0 ? Integer.parseInt(args[0]) : 8080;
        BackendState state = new BackendState();
        state.seed();
        ApiServer server = new ApiServer(state);
        server.start(port);
        System.out.println("Home Affairs backend running on http://localhost:" + port);
        Thread.currentThread().join();
    }
}