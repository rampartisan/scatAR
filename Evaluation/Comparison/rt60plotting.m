function rt60plotting(cfsT30T20data,plotOpt)
% plots rt60 t20 and t30 data on the same graph


% split input array into centrefrequencies, rt t20 and rt t30 values

cfs = cfsT30T20data(:,1);
t30 = cfsT30T20data(:,2);
t20 = cfsT30T20data(:,3);

% plot both on the same graph, set axes properties and labels etc.
if plotOpt == 1
    semilogx(cfs,t30,'-o','color','k','MarkerSize',10, 'MarkerFaceColor','k')
elseif plotOpt == 2
    semilogx(cfs,t30,'-s','color','k','MarkerSize',10, 'MarkerFaceColor','k')
elseif plotOpt == 3
        semilogx(cfs,t30,'-^','color','k','MarkerSize',10, 'MarkerFaceColor','k')
end
    
% hold on;
% plot(t20,'-s','color','k');
% set(gca,'Xtick',cfs(1:end));
% set(gca,'XtickLabel',cfs);
xlabel('Frequency (Hz)')
ylabel('T30 (s)');
% legend('T30');
grid on;
hold off;

end
